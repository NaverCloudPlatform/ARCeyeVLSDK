import MetalKit
import simd

class ColoredCube {
    /// The MTLDevice used for creating buffers, textures, and pipeline states.
    private let device: MTLDevice

    /// The custom render pipeline state for drawing this cube.
    private var pipelineState: MTLRenderPipelineState!
    
    /// A depth stencil state configured for typical 3D depth testing.
    private var depthState: MTLDepthStencilState!
    
    /// A buffer containing the vertex data for the cube.
    private var vertexBuffer: MTLBuffer!
    
    /// A buffer containing the index data that defines the faces of the cube.
    private var indexBuffer: MTLBuffer!
    
    /// A buffer that holds uniform data, such as the MVP (model-view-projection) matrix.
    private var uniformBuffer: MTLBuffer!

    /// The total number of indices used when drawing the cube.
    private var indexCount: Int = 0
    
    /// The cube’s position in 3D space.
    private var position = simd_float3(0.0, 0.0, 0.0)
    
    /// The cube’s rotation in degrees around the X, Y, and Z axes.
    private var rotation = simd_float3(0.0, 0.0, 0.0)
    
    /// The cube’s scale factors along the X, Y, and Z axes.
    private var scale    = simd_float3(1.0, 1.0, 1.0)
    
    // MARK: - Initialization
    
    /// Initializes a `ColoredCube` with the specified Metal device.
    /// - Parameter device: The `MTLDevice` used for GPU operations.
    init(device: MTLDevice) {
        self.device = device
        
        setupPipeline()
        setupCubeGeometry()
    }
    
    // MARK: - Rendering
    
    /// Renders the colored cube by applying transformations and issuing draw calls.
    /// - Parameters:
    ///   - encoder: A `MTLRenderCommandEncoder` to record draw commands.
    ///   - viewMatrix: A 4x4 matrix (in double precision) representing the camera's view transform.
    ///   - projMatrix: A 4x4 matrix representing the camera's projection transform.
    func render(encoder: MTLRenderCommandEncoder,
                viewMatrix: simd_double4x4,
                projMatrix: simd_float4x4)
    {
        // Set the pipeline and depth states for rendering.
        encoder.setRenderPipelineState(pipelineState)
        encoder.setDepthStencilState(depthState)
        
        // Convert the view matrix from double to float for consistency with the rest of the pipeline.
        let viewMatrixF = simd_float4x4(
            simd_float4(viewMatrix.columns.0),
            simd_float4(viewMatrix.columns.1),
            simd_float4(viewMatrix.columns.2),
            simd_float4(viewMatrix.columns.3)
        )
        
        // Compute the model matrix from position, rotation, and scale.
        let modelMatrix = makeModelMatrix(position: position,
                                          rotation: rotation,
                                          scale: scale)
        
        // Combine the projection, view, and model matrices into one MVP matrix.
        let mvp = projMatrix * viewMatrixF * modelMatrix
        
        // Copy the MVP matrix into the uniform buffer.
        var uniforms = Uniforms3D(mvpMatrix: mvp)
        memcpy(uniformBuffer.contents(), &uniforms, MemoryLayout<Uniforms3D>.size)
        
        // Bind the vertex and uniform buffers to the vertex stage.
        encoder.setVertexBuffer(vertexBuffer, offset: 0, index: 0)
        encoder.setVertexBuffer(uniformBuffer, offset: 0, index: 1)
        
        // Draw the cube geometry using indexed triangles.
        encoder.drawIndexedPrimitives(type: .triangle,
                                      indexCount: indexCount,
                                      indexType: .uint16,
                                      indexBuffer: indexBuffer,
                                      indexBufferOffset: 0)
    }
    
    // MARK: - Transform Setters
    
    /// Updates the cube’s position in 3D space.
    /// - Parameter newPosition: A `simd_float3` specifying the new position.
    func setPosition(_ newPosition: simd_float3) {
        position = newPosition
    }
    
    /// Updates the cube’s rotation in degrees around the X, Y, and Z axes.
    /// - Parameter newRotation: A `simd_float3` specifying the new rotation angles (in degrees).
    func setRotation(_ newRotation: simd_float3) {
        rotation = newRotation
    }
    
    /// Updates the cube’s scale factors along the X, Y, and Z axes.
    /// - Parameter newScale: A `simd_float3` specifying the new scale values.
    func setScale(_ newScale: simd_float3) {
        scale = newScale
    }
}

// MARK: - Setup Methods

extension ColoredCube {
    
    /// Configures and creates the Metal pipeline and depth state objects used for rendering.
    private func setupPipeline() {
        // Load the default Metal library (contains our shader functions).
        guard let library = device.makeDefaultLibrary() else {
            fatalError("Failed to load default Metal library.")
        }
        
        // Create the vertex and fragment functions to be used by the pipeline.
        let vertexFunc   = library.makeFunction(name: "cubeVertexShader")
        let fragmentFunc = library.makeFunction(name: "cubeFragmentShader")
        
        // Set up a descriptor for configuring the pipeline.
        let pipelineDesc = MTLRenderPipelineDescriptor()
        pipelineDesc.label = "ColoredCubePipeline"
        pipelineDesc.vertexFunction   = vertexFunc
        pipelineDesc.fragmentFunction = fragmentFunc
        
        // Match the color and depth formats to your Metal view or render target.
        pipelineDesc.colorAttachments[0].pixelFormat = .bgra8Unorm_srgb
        pipelineDesc.depthAttachmentPixelFormat      = .depth32Float
        
        // Define a vertex descriptor that describes how vertex data is laid out.
        let vertexDescriptor = MTLVertexDescriptor()
        
        // Positions (float3) are in attribute(0).
        vertexDescriptor.attributes[0].format = .float3
        vertexDescriptor.attributes[0].offset = 0
        vertexDescriptor.attributes[0].bufferIndex = 0
        
        // Colors (float3) are in attribute(1).
        vertexDescriptor.attributes[1].format = .float3
        vertexDescriptor.attributes[1].offset = MemoryLayout<Float>.size * 3
        vertexDescriptor.attributes[1].bufferIndex = 0
        
        // Each vertex has 6 floats total: 3 for position, 3 for color.
        vertexDescriptor.layouts[0].stride = MemoryLayout<Float>.size * 6
        vertexDescriptor.layouts[0].stepRate = 1
        vertexDescriptor.layouts[0].stepFunction = .perVertex
        
        pipelineDesc.vertexDescriptor = vertexDescriptor
        
        // Build the render pipeline state.
        do {
            pipelineState = try device.makeRenderPipelineState(descriptor: pipelineDesc)
        } catch {
            fatalError("Failed to create pipelineState: \(error)")
        }
        
        // Configure a depth stencil state to allow proper 3D occlusion.
        let depthDesc = MTLDepthStencilDescriptor()
        depthDesc.depthCompareFunction = .less
        depthDesc.isDepthWriteEnabled  = true
        depthState = device.makeDepthStencilState(descriptor: depthDesc)
    }
    
    /// Creates a cube’s vertex and index buffers, along with a buffer for storing uniform data.
    private func setupCubeGeometry() {
        let size: Float = 1.0
        
        // Each face of the cube has 4 vertices, each with position (x,y,z) and color (r,g,b).
        let vertices: [Float] = [
            // Front face (RED)
            -size, -size, +size,   1, 0, 0,  // 0
             size, -size, +size,   1, 0, 0,  // 1
             size,  size, +size,   1, 0, 0,  // 2
            -size,  size, +size,   1, 0, 0,  // 3

            // Back face (GREEN)
             size, -size, -size,   0, 1, 0,  // 4
            -size, -size, -size,   0, 1, 0,  // 5
            -size,  size, -size,   0, 1, 0,  // 6
             size,  size, -size,   0, 1, 0,  // 7

            // Left face (BLUE)
            -size, -size, -size,   0, 0, 1,  // 8
            -size, -size, +size,   0, 0, 1,  // 9
            -size,  size, +size,   0, 0, 1,  // 10
            -size,  size, -size,   0, 0, 1,  // 11

            // Right face (YELLOW)
             size, -size, +size,   1, 1, 0,  // 12
             size, -size, -size,   1, 1, 0,  // 13
             size,  size, -size,   1, 1, 0,  // 14
             size,  size, +size,   1, 1, 0,  // 15

            // Top face (PINK)
            -size,  size, +size,   1, 0, 1,  // 16
             size,  size, +size,   1, 0, 1,  // 17
             size,  size, -size,   1, 0, 1,  // 18
            -size,  size, -size,   1, 0, 1,  // 19

            // Bottom face (CYAN)
            -size, -size, -size,   0, 1, 1,  // 20
             size, -size, -size,   0, 1, 1,  // 21
             size, -size, +size,   0, 1, 1,  // 22
            -size, -size, +size,   0, 1, 1,  // 23
        ]
        
        // Indices defining two triangles per face (6 faces total).
        let indices: [UInt16] = [
            // Front
            0, 1, 2,   2, 3, 0,
            // Back
            4, 5, 6,   6, 7, 4,
            // Left
            8, 9, 10,  10, 11, 8,
            // Right
            12, 13, 14, 14, 15, 12,
            // Top
            16, 17, 18, 18, 19, 16,
            // Bottom
            20, 21, 22, 22, 23, 20
        ]
        
        // Store the index count for use during draw calls.
        self.indexCount = indices.count
        
        // Create the vertex buffer from the vertex array.
        vertexBuffer = device.makeBuffer(bytes: vertices,
                                         length: vertices.count * MemoryLayout<Float>.size,
                                         options: [])
        
        // Create the index buffer from the index array.
        indexBuffer = device.makeBuffer(bytes: indices,
                                        length: indices.count * MemoryLayout<UInt16>.size,
                                        options: [])
        
        // Create a buffer for our Uniforms3D struct (contains the MVP matrix).
        uniformBuffer = device.makeBuffer(length: MemoryLayout<Uniforms3D>.size,
                                          options: [])
    }
    
    /// Creates a model matrix from position, rotation, and scale vectors.
    /// - Parameters:
    ///   - position: A `simd_float3` specifying translation along X, Y, Z.
    ///   - rotation: A `simd_float3` specifying rotation (in degrees) around X, Y, Z.
    ///   - scale: A `simd_float3` specifying scale factors along X, Y, Z.
    /// - Returns: A `simd_float4x4` model matrix.
    private func makeModelMatrix(position: simd_float3,
                                 rotation: simd_float3,
                                 scale: simd_float3) -> simd_float4x4
    {
        // Start with the identity matrix for translation.
        var tMat = matrix_identity_float4x4
        tMat.columns.3 = simd_float4(position.x,
                                     position.y,
                                     position.z,
                                     1.0)
        
        // Rotate around X axis.
        let radianX = rotation.x * Float.pi / 180.0
        var rotX = matrix_identity_float4x4
        rotX.columns.1.y = cosf(radianX)
        rotX.columns.1.z = sinf(radianX)
        rotX.columns.2.y = -sinf(radianX)
        rotX.columns.2.z = cosf(radianX)
        
        // Rotate around Y axis.
        let radianY = rotation.y * Float.pi / 180.0
        var rotY = matrix_identity_float4x4
        rotY.columns.0.x = cosf(radianY)
        rotY.columns.0.z = -sinf(radianY)
        rotY.columns.2.x = sinf(radianY)
        rotY.columns.2.z = cosf(radianY)
        
        // Rotate around Z axis.
        let radianZ = rotation.z * Float.pi / 180.0
        var rotZ = matrix_identity_float4x4
        rotZ.columns.0.x = cosf(radianZ)
        rotZ.columns.0.y = sinf(radianZ)
        rotZ.columns.1.x = -sinf(radianZ)
        rotZ.columns.1.y = cosf(radianZ)
        
        // Scale along X, Y, and Z axes.
        var sMat = matrix_identity_float4x4
        sMat.columns.0.x = scale.x
        sMat.columns.1.y = scale.y
        sMat.columns.2.z = scale.z
        
        // Combine translation, rotation (Z * Y * X), and scaling.
        return tMat * (rotZ * rotY * rotX) * sMat
    }
}
