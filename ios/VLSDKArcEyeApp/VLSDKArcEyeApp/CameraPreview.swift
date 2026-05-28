import Foundation
import Metal
import MetalKit
import simd
import CoreVideo

class CameraPreview {
    /// The MTLDevice used for creating buffers, textures, and pipeline states.
    private let device: MTLDevice
    
    /// Our custom render pipeline state for YCbCr camera rendering.
    private var pipelineState: MTLRenderPipelineState!
    
    /// A depth stencil state configured so that the camera feed always renders on top
    /// (i.e., using .always compare function).
    private var depthState: MTLDepthStencilState!
    
    /// A Core Video texture cache for converting CVPixelBuffer to MTLTexture efficiently.
    private var textureCache: CVMetalTextureCache?
    
    /// A buffer holding the geometry for a simple full-screen quad.
    private var quadVertexBuffer: MTLBuffer!
    
    /// A buffer that stores Uniforms data, including a texture transform matrix.
    private var uniformsBuffer: MTLBuffer!
    
    /// Stores the most recent camera pixel buffer.
    private var currentPixelBuffer: CVPixelBuffer?
    
    /// Stores the most recent texture transform for properly orienting the camera image.
    private var currentTransform = matrix_identity_float3x3
    
    // MARK: - Initialization
    
    /// Initializes a CameraPreview with the specified Metal device.
    /// - Parameter device: The MTLDevice for GPU operations.
    init(device: MTLDevice) {
        self.device = device
        setupMetal()
    }
    
    /// Renders the camera feed by using the current pixel buffer to create Y/CbCr textures,
    /// then drawing a full-screen quad via our pipeline.
    /// - Parameters:
    ///   - encoder: The MTLRenderCommandEncoder to append draw commands.
    ///   - pixelBuffer: The new camera frame to display.
    ///   - transform: A 3x3 matrix that transforms texture coordinates (e.g., rotation/flip).
    func render(encoder: MTLRenderCommandEncoder, texTransform: simd_float3x3, pixelBuffer: CVPixelBuffer) {
        self.currentTransform = texTransform
        self.currentPixelBuffer = pixelBuffer
        
        // Update the transform inside the uniforms buffer for use in the vertex shader.
        let uniformsPtr = uniformsBuffer.contents().bindMemory(to: Uniforms.self, capacity: 1)
        uniformsPtr.pointee.textureTransform = currentTransform
        
        // Safely unwrap the current pixel buffer.
        guard let pb = currentPixelBuffer else { return }
        
        // Attempt to create Metal textures for the Y and CbCr planes.
        guard let texY = makeTexture(from: pb, planeIndex: 0, pixFormat: .r8Unorm),
              let texCbCr = makeTexture(from: pb, planeIndex: 1, pixFormat: .rg8Unorm) else {
            return
        }
        
        // Set up our custom pipeline for rendering the camera feed.
        encoder.setRenderPipelineState(pipelineState)
        encoder.setDepthStencilState(depthState)
        
        // Bind vertex buffers for the full-screen quad.
        encoder.setVertexBuffer(quadVertexBuffer, offset: 0, index: Int(kBufferIndexPlaneVertex.rawValue))
        encoder.setVertexBuffer(uniformsBuffer,   offset: 0, index: Int(kBufferIndexUniforms.rawValue))
        
        // Bind the two textures for Y and CbCr planes, respectively.
        encoder.setFragmentTexture(texY,    index: Int(kTextureIndexY.rawValue))
        encoder.setFragmentTexture(texCbCr, index: Int(kTextureIndexCbCr.rawValue))
        
        // Draw the quad as a triangle strip with 4 vertices.
        encoder.drawPrimitives(type: .triangleStrip, vertexStart: 0, vertexCount: 4)
    }
    
    // MARK: - Setup
    
    /// Configures Metal resources such as the pipeline state, depth state,
    /// vertex buffers, and the CoreVideo texture cache.
    private func setupMetal() {
        // Create a CoreVideo Metal texture cache for efficient pixel buffer -> texture conversion.
        var cache: CVMetalTextureCache?
        CVMetalTextureCacheCreate(nil, nil, device, nil, &cache)
        self.textureCache = cache
        
        // Load default library and create vertex/fragment functions for camera preview.
        guard let library = device.makeDefaultLibrary() else {
            fatalError("Failed to load default Metal library.")
        }
        let vertexFunc   = library.makeFunction(name: "cameraPreviewVertexShader")!
        let fragmentFunc = library.makeFunction(name: "cameraPreviewFragmentShader")!
        
        // Configure a render pipeline descriptor.
        let pipelineDesc = MTLRenderPipelineDescriptor()
        pipelineDesc.label = "CameraPreviewPipeline"
        pipelineDesc.vertexFunction   = vertexFunc
        pipelineDesc.fragmentFunction = fragmentFunc
        
        // Define the color attachment pixel format; must match your view or render target.
        pipelineDesc.colorAttachments[0].pixelFormat = .bgra8Unorm_srgb
        
        // Define the depth attachment pixel format.
        // Matches the SceneKit default of .depth32Float in this example.
        pipelineDesc.depthAttachmentPixelFormat = .depth32Float
        
        // Create a vertex descriptor to interpret the layout of the quad vertices.
        let vertexDescriptor = MTLVertexDescriptor()
        
        // attribute(0) -> position (float2)
        vertexDescriptor.attributes[0].format = .float2
        vertexDescriptor.attributes[0].offset = 0
        vertexDescriptor.attributes[0].bufferIndex = Int(kBufferIndexPlaneVertex.rawValue)
        
        // attribute(1) -> texture coordinate (float2)
        vertexDescriptor.attributes[1].format = .float2
        vertexDescriptor.attributes[1].offset = MemoryLayout<Float>.size * 2
        vertexDescriptor.attributes[1].bufferIndex = Int(kBufferIndexPlaneVertex.rawValue)
        
        // Each vertex has 4 floats: 2 for position, 2 for texCoords.
        vertexDescriptor.layouts[Int(kBufferIndexPlaneVertex.rawValue)].stride = MemoryLayout<Float>.size * 4
        vertexDescriptor.layouts[Int(kBufferIndexPlaneVertex.rawValue)].stepRate = 1
        vertexDescriptor.layouts[Int(kBufferIndexPlaneVertex.rawValue)].stepFunction = .perVertex
        
        pipelineDesc.vertexDescriptor = vertexDescriptor
        
        // Build the render pipeline state.
        do {
            pipelineState = try device.makeRenderPipelineState(descriptor: pipelineDesc)
        } catch {
            fatalError("Failed to create pipelineState: \(error)")
        }
        
        // Configure a depth stencil state that always passes, ensuring the camera feed
        // is rendered regardless of depth. (Typically placed in the background.)
        let depthDesc = MTLDepthStencilDescriptor()
        depthDesc.depthCompareFunction = .always
        depthDesc.isDepthWriteEnabled  = false
        self.depthState = device.makeDepthStencilState(descriptor: depthDesc)
        
        // Define a full-screen quad as a triangle strip:
        //
        //   (-1, -1) ---- (1, -1)
        //      |             |
        //   (-1,  1) ---- (1,  1)
        //
        // Each vertex has position.xy, texCoord.xy
        let quadVertices: [Float] = [
            // position        texCoord
            -1, -1,           0, 1,
             1, -1,           1, 1,
            -1,  1,           0, 0,
             1,  1,           1, 0
        ]
        
        // Create a buffer for the quad vertex data.
        quadVertexBuffer = device.makeBuffer(bytes: quadVertices,
                                             length: quadVertices.count * MemoryLayout<Float>.size,
                                             options: [])
        
        // Create a buffer to store Uniforms (e.g. texture transform).
        uniformsBuffer = device.makeBuffer(length: MemoryLayout<Uniforms>.size, options: [])
    }
    
    /// Creates a Metal texture from a plane of the provided CVPixelBuffer using the texture cache.
    /// - Parameters:
    ///   - pixelBuffer: The source pixel buffer.
    ///   - planeIndex: The plane index (0 = Y, 1 = CbCr for 420v format).
    ///   - pixFormat: The Metal pixel format that corresponds to the plane.
    /// - Returns: An optional MTLTexture if creation succeeded.
    private func makeTexture(from pixelBuffer: CVPixelBuffer,
                             planeIndex: Int,
                             pixFormat: MTLPixelFormat) -> MTLTexture? {
        guard let cache = textureCache else { return nil }
        let width  = CVPixelBufferGetWidthOfPlane(pixelBuffer, planeIndex)
        let height = CVPixelBufferGetHeightOfPlane(pixelBuffer, planeIndex)
        
        var ctRef: CVMetalTexture? = nil
        let status = CVMetalTextureCacheCreateTextureFromImage(nil,
                                                               cache,
                                                               pixelBuffer,
                                                               nil,
                                                               pixFormat,
                                                               width,
                                                               height,
                                                               planeIndex,
                                                               &ctRef)
        
        if status == kCVReturnSuccess, let ct = ctRef,
           let texture = CVMetalTextureGetTexture(ct) {
            return texture
        }
        return nil
    }
}
