#ifndef METALLIC_ROUGHNESS_ARLABS_INCLUDED
#define METALLIC_ROUGHNESS_ARLABS_INCLUDED

#pragma target 3.0

sampler2D _normalTexture;

sampler2D _occlusionTexture;

float4 _emissiveFactor;
sampler2D _emissiveTexture;

sampler2D _baseColorTexture;
float4 _baseColorFactor;

half _roughnessFactor;
half _metallicFactor;
sampler2D _metallicRoughnessTexture;

struct Input
{
    // Note: The `uv` prefix "magically" maps to
    // the corresponding shader property, e.g.
    // `uv_baseColorTexture` -> `_baseColorTexture`.

    float2 uv_baseColorTexture;
    float2 uv_normalTexture;
    float2 uv_occlusionTexture;
    float2 uv_emissiveTexture;
    float2 uv_metallicRoughnessTexture;

    // Note: COLOR faults to (1,1,1,1) if unset

    float4 vertexColor : COLOR;

    // Note: VFACE is used to implement double-sided
    // rendering of triangles. The value of VFACE is
    // positive when a triangle is front-facing and negative when
    // a triangle is back-facing. See VFACE section of
    // https://docs.unity3d.com/Manual/SL-ShaderSemantics.html
    // and also a related discussion at
    // https://forum.unity.com/threads/standard-shader-modified-to-be-double-sided-is-very-shiny-on-the-underside.393068/

    fixed vface : VFACE;
};

void surf (Input IN, inout SurfaceOutputStandard o)
{
    fixed4 c = tex2D (_baseColorTexture, IN.uv_baseColorTexture) * IN.vertexColor * _baseColorFactor;
    o.Albedo = c.rgb;
    o.Alpha = c.a;
    o.Normal = UnpackNormal(tex2D(_normalTexture, IN.uv_normalTexture));
    if (IN.vface < 0)
        o.Normal.z *= -1.0;
    o.Occlusion = tex2D (_occlusionTexture, IN.uv_occlusionTexture).r;
    o.Emission = tex2D (_emissiveTexture, IN.uv_emissiveTexture) * _emissiveFactor;
    o.Metallic = tex2D(_metallicRoughnessTexture, IN.uv_metallicRoughnessTexture).b * _metallicFactor;
    o.Smoothness = 1.0 - tex2D(_metallicRoughnessTexture, IN.uv_metallicRoughnessTexture).g * _roughnessFactor;
}

#endif