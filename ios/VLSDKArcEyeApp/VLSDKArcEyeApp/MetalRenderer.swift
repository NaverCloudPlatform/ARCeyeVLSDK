import MetalKit
import simd
import CoreVideo

class MetalRenderer: NSObject {
    /// The MTLDevice represents the GPU. We need it to create resources.
    private let device: MTLDevice
    
    /// A queue used to schedule and execute GPU commands.
    private let commandQueue: MTLCommandQueue
    
    /// Called when the viewport (drawable size) has changed, passing new width and height.
    /// Used here to notify the VLSDK so it can adjust its viewport rendering.
    var hasChangedViewport: ((CGFloat, CGFloat) -> Void)?
    
    /// Keeps track of the last known viewport size to detect changes.
    private var lastViewportSize = CGSize.zero

    /// A custom class responsible for rendering colored cube.
    private var cubeYAngle:Float = 0.0
    
    /// Additional cubes added via raycasting.
    private var hitCubes: [ColoredCube] = []
    
    /// A custom class responsible for rendering camera frames.
    private let cameraPreview: CameraPreview
    
    /// Matrices for the current view (camera/model transformation) and projection (for 3D projection).
    private var currentViewMatrix: simd_double4x4 = matrix_identity_double4x4
    private var currentProjMatrix: simd_float4x4 = matrix_identity_float4x4
    private var currentTextureTransform: simd_float3x3 = matrix_identity_float3x3
    private var currentPixelBuffer: CVPixelBuffer? = nil
    
    init?(mtkView: MTKView) {
        guard let device = MTLCreateSystemDefaultDevice() else {
            return nil
        }
        self.device = device

        guard let commandQueue = device.makeCommandQueue() else {
            return nil
        }
        self.commandQueue = commandQueue
        
        self.cameraPreview = CameraPreview(device: device)
        
        super.init()
        
        /// Configure the MTKView with our device and desired pixel formats.
        mtkView.device = device
        mtkView.colorPixelFormat = .bgra8Unorm_srgb
        mtkView.depthStencilPixelFormat = .depth32Float
        mtkView.delegate = self
        mtkView.framebufferOnly = true
    }
    
    // MARK: - Public Methods
    
    func update(viewMatrix: simd_double4x4, projMatrix: simd_float4x4, texTransform: simd_float3x3, pixelBuffer: CVPixelBuffer) {
        self.currentViewMatrix = viewMatrix
        self.currentProjMatrix = projMatrix
        self.currentTextureTransform = texTransform
        self.currentPixelBuffer = pixelBuffer
    }
    
    /// Adds a new colored cube at the specified position.
    /// - Parameter position: The 3D position where the cube should be placed.
    func addCube(at position: simd_float3) {
        let cube = ColoredCube(device: device)
        cube.setPosition(position)
        cube.setScale(simd_make_float3(0.01, 0.01, 0.01))
        hitCubes.append(cube)
    }
}

// MARK: - MTKViewDelegate

extension MetalRenderer: MTKViewDelegate {
    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {
        /// You could update projection matrices or other render configurations here based on the new size.
    }
    
    func draw(in view: MTKView) {
        /// Ensure there is a valid drawable to render into.
        guard let drawable = view.currentDrawable else { return }
        
        /// Obtain the current render pass descriptor, which configures the render pipeline.
        guard let renderPassDescriptor = view.currentRenderPassDescriptor else { return }
        
        let currentSize = view.drawableSize
        if currentSize != lastViewportSize {
            lastViewportSize = currentSize
            hasChangedViewport?(currentSize.width, currentSize.height)
        }
        
        /// Set the clear color for the color attachment. This is the background color at the start of the frame.
        renderPassDescriptor.colorAttachments[0].clearColor = MTLClearColorMake(0.2, 0.2, 0.2, 1.0)
        /// Specify how to handle the existing color data. Here, we clear it.
        renderPassDescriptor.colorAttachments[0].loadAction = .clear
        /// Specify how to store the rendered data. Here, we store it for display.
        renderPassDescriptor.colorAttachments[0].storeAction = .store
        
        /// Create a command buffer from the command queue. A command buffer holds GPU work.
        guard let commandBuffer = commandQueue.makeCommandBuffer() else { return }
        /// Create a render command encoder to encode drawing commands into the command buffer.
        guard let renderEncoder = commandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor) else { return }
        
        /// Render the camera preview. This might draw a textured quad or some other shape showing the camera image.
        if let pixelBuffer = currentPixelBuffer {
            cameraPreview.render(encoder: renderEncoder,
                                 texTransform: currentTextureTransform,
                                 pixelBuffer: pixelBuffer)
        }
        
        /// Render the colored cube.
        var newAngle = cubeYAngle + 2.0
        if(newAngle >= 360.0) {
            newAngle -= 360.0
        }
        cubeYAngle = newAngle
        
        /// Render all hit cubes.
        for cube in hitCubes {
            cube.setRotation(simd_make_float3(0.0, newAngle, 0.0))
            cube.render(encoder: renderEncoder,
                        viewMatrix: currentViewMatrix,
                        projMatrix: currentProjMatrix)
        }
        
        /// End encoding after finishing all drawing commands.
        renderEncoder.endEncoding()
        
        /// Present the drawable to the screen at the next vsync.
        commandBuffer.present(drawable)
        /// Commit the command buffer so it can be executed by the GPU.
        commandBuffer.commit()
    }
}
