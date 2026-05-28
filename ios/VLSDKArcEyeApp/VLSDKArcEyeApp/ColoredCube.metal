#include <metal_stdlib>
#include <simd/simd.h>
#include "ShaderTypes.h"

using namespace metal;

/// A structure that represents the per-vertex input data for the cube.
/// Includes the position and color attributes.
struct CubeVertexIn {
    /// The vertex position in 3D space.
    float3 position [[attribute(0)]];
    
    /// The vertex color in RGB format.
    float3 color    [[attribute(1)]];
};

/// A structure that represents the data passed from the vertex shader to the fragment shader.
/// Includes the clip-space position and the interpolated color.
struct CubeVertexOut {
    /// The transformed vertex position, which will be used for rasterization.
    float4 position [[position]];
    
    /// The interpolated color passed to the fragment stage.
    float3 color;
};

/// The vertex function for rendering a colored cube.
/// Transforms each vertex by the provided MVP (model-view-projection) matrix.
vertex CubeVertexOut cubeVertexShader(
    CubeVertexIn       inVertex    [[stage_in]],
    constant Uniforms3D& uniforms  [[buffer(1)]]
)
{
    CubeVertexOut out;
    // Multiply the vertex position by the MVP matrix to obtain clip-space coordinates.
    out.position = uniforms.mvpMatrix * float4(inVertex.position, 1.0);
    
    // Pass the original color through to the fragment stage unchanged.
    out.color    = inVertex.color;
    
    return out;
}

/// The fragment function for rendering a colored cube.
fragment float4 cubeFragmentShader(CubeVertexOut in [[stage_in]])
{
    // Convert the interpolated color to an RGBA value with full alpha.
    return float4(in.color, 1.0);
}
