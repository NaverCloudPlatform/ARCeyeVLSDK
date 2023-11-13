Shader "ARLabs/FadeTexture"
{
    Properties
    {
        _fade ("Fade", Range(0.0, 1.0)) = 1.0

        _normalTexture("Normal Map", 2D) = "bump" {}

        _occlusionTexture("Occlusion Map", 2D) = "white" {}

        _emissiveFactor("Emissive Factor", Color) = (1, 1, 1, 1)
        _emissiveTexture("Emission Map", 2D) = "black" {}

        _baseColorFactor("Base Color Factor", Color) = (1, 1, 1, 1)
        _baseColorTexture("Base Color Texture", 2D) = "white" {}

        _roughnessFactor("Roughness Factor", Range(0,1)) = 1.0
        _metallicFactor("Metallic Factor", Range(0,1)) = 1.0
        _metallicRoughnessTexture("Metallic Roughness Texture", 2D) = "white" {}
    }
    SubShader
    {
        // 2pass 기반의 fade shader.
        // 참고 자료 : https://darkcatgame.tistory.com/31?category=806332
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        blend SrcAlpha OneMinusSrcAlpha

        // 기본적인 transparent shader는 z buffer를 기록하지 않는다.
        // 이로 인해 alpha 적용 시 모델 안쪽의 삼각형들이 보이게 된다.
        // 아래와 같은 비어있는 패스를 통해서 z buffer 기반의 depth culling이 먼저 일어나게 한다.
        Pass { 
            ColorMask 0 
        }

        // depth culling 이후 렌더링 실행.
        // PBR 렌더링 pass
        zwrite off
        CGPROGRAM

        #pragma target 3.0
        #pragma surface surf Standard alpha:fade noshadow nolightmap nofog nometa nolppv

        sampler2D _normalTexture;

        sampler2D _occlusionTexture;

        float4 _emissiveFactor;
        sampler2D _emissiveTexture;

        sampler2D _baseColorTexture;
        float4 _baseColorFactor;

        half _roughnessFactor;
        half _metallicFactor;
        sampler2D _metallicRoughnessTexture;

        float _fade;

        struct Input
        {
            float2 uv_baseColorTexture;
            float2 uv_normalTexture;
            float2 uv_occlusionTexture;
            float2 uv_emissiveTexture;
            float2 uv_metallicRoughnessTexture;

            float4 vertexColor : COLOR;

            fixed vface : VFACE;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_baseColorTexture, IN.uv_baseColorTexture) * IN.vertexColor * _baseColorFactor;
            o.Albedo = c.rgb;
            o.Alpha = _fade;
            o.Normal = UnpackNormal(tex2D(_normalTexture, IN.uv_normalTexture));
            if (IN.vface < 0)
                o.Normal.z *= -1.0;
            o.Occlusion = tex2D(_occlusionTexture, IN.uv_occlusionTexture).r;
            o.Emission = tex2D(_emissiveTexture, IN.uv_emissiveTexture) * _emissiveFactor;
            o.Metallic = tex2D(_metallicRoughnessTexture, IN.uv_metallicRoughnessTexture).b * _metallicFactor;
            o.Smoothness = 1.0 - tex2D(_metallicRoughnessTexture, IN.uv_metallicRoughnessTexture).g * _roughnessFactor;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
