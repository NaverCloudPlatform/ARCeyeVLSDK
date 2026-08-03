Shader "ARDataset/FlipY"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"  // ✅ 이게 중요함!

            sampler2D _MainTex;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_img v) {  // appdata_img = Unity가 제공하는 기본 구조체
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = float2(v.texcoord.x, 1.0 - v.texcoord.y); // ✅ Y축 플립
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}
