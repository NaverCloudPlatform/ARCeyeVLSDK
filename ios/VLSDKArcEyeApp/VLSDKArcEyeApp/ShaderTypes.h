#ifndef ShaderTypes_h
#define ShaderTypes_h

#include <simd/simd.h>

typedef enum BufferIndices {
    kBufferIndexPlaneVertex     = 0,
    kBufferIndexUniforms        = 1
} BufferIndices;

typedef enum TextureIndices {
    kTextureIndexY    = 0,
    kTextureIndexCbCr = 1
} TextureIndices;

typedef struct {
    simd_float2 position;
    simd_float2 texCoord;
} ImageVertex;

typedef struct {
    matrix_float3x3 textureTransform;
} Uniforms;

typedef struct {
    matrix_float4x4 mvpMatrix;
} Uniforms3D;

#endif /* ShaderTypes_h */
