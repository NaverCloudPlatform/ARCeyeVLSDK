#include <metal_stdlib>
#include <simd/simd.h>
#include "ShaderTypes.h"

using namespace metal;

/// Structure describing the layout of vertex input attributes.
struct VertexIn {
    float2 position  [[attribute(0)]];
    float2 texCoord  [[attribute(1)]];
};

/// Structure passed from the vertex shader to the fragment shader.
struct VertexOut {
    float4 position [[position]];
    float2 texCoord;
};

/// Vertex function that transforms each vertex to clip space
/// and applies a 3x3 texture transform matrix for orientation.
vertex VertexOut cameraPreviewVertexShader(
    VertexIn               in          [[stage_in]],
    constant Uniforms&     uniforms    [[buffer(kBufferIndexUniforms)]]
)
{
    VertexOut out;
    
    // Position is directly mapped to clip space since it's normalized in [-1,1].
    out.position = float4(in.position, 0.0, 1.0);
    
    // Apply the 3x3 texture transform matrix to the incoming texCoord.
    float3 uv = float3(in.texCoord, 1.0);
    float3 transformedUV = uniforms.textureTransform * uv;
    out.texCoord = float2(transformedUV.x, transformedUV.y);
    
    return out;
}

/// Fragment function that samples the Y and CbCr planes, converts to RGB, and outputs the final color.
fragment float4 cameraPreviewFragmentShader(
    VertexOut                         in           [[stage_in]],
    texture2d<float, access::sample>  texY         [[texture(kTextureIndexY)]],
    texture2d<float, access::sample>  texCbCr      [[texture(kTextureIndexCbCr)]]
)
{
    // Sampler configuration. Adjust as necessary for your color or performance needs.
    constexpr sampler colorSampler(mip_filter::linear,
                                   mag_filter::linear,
                                   min_filter::linear);

    // Conversion matrix for YCbCr to RGB
    const float4x4 ycbcrToRGBTransform = float4x4(
        float4(+1.0,   +1.0,   +1.0,   0.0),
        float4(+0.0,   -0.344, +1.772, 0.0),
        float4(+1.402, -0.714, +0.0,   0.0),
        float4(-0.701, +0.529, -0.886, +1.0)
    );
    
    // Sample the textures.
    // Y-plane gives a single channel (R), and CbCr plane gives two channels (RG).
    float y = texY.sample(colorSampler, in.texCoord).r;
    float2 cbcr = texCbCr.sample(colorSampler, in.texCoord).rg;
    
    // Pack them into a float4 for matrix multiplication.
    float4 yCbCr = float4(y, cbcr.x, cbcr.y, 1.0);
    
    // Convert from YCbCr to RGB.
    float4 color = ycbcrToRGBTransform * yCbCr;
    
    // Convert from sRGB to linear by applying gamma correction (~2.2).
    color.rgb = pow(color.rgb, 2.2);
    
    return color;
}
