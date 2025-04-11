Shader "WaveMaker/NormalRenderer"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {} 
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Fog { Mode off }
        LOD 100

        Pass
        {
            name "RenderNormals"

            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct v2f
            {
                float4 vertex : POSITION;
                float3 normal : TEXCOORD3;
            };

            v2f vert (float4 vertex : POSITION, float3 normal : TEXCOORD3)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.normal = normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fix interpolation by normalizing interpolated normal. Then normalize to color value 0-1
                float3 normal = normalize(i.normal) * 0.5 + 0.5;

                // Change Y by Z so that our object space is the same as the tangent space for all vertices and we store a correct Normal Map.
                return float4(normal.xzy, 1);
            }
            ENDHLSL
        }

        Pass
        {
            name "BoxBlur"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            // x contains 1.0/width, y contains 1.0/height, z contains width, w contains height
            float4 _MainTex_TexelSize;
            float BlurAmount;


            struct v2f
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD3;
            };

            v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD3)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Box Blur
                
                half3 centerCol = UNITY_SAMPLE_TEX2D(_MainTex, i.uv).xyz;
                
                half3 topCol = UNITY_SAMPLE_TEX2D(_MainTex, float2(i.uv.x, i.uv.y + _MainTex_TexelSize.y)).xyz;
                half3 botCol = UNITY_SAMPLE_TEX2D(_MainTex, float2(i.uv.x, i.uv.y - _MainTex_TexelSize.y)).xyz;
                half3 leftCol = UNITY_SAMPLE_TEX2D(_MainTex, float2(i.uv.x - _MainTex_TexelSize.x, i.uv.y)).xyz;
                half3 rightCol = UNITY_SAMPLE_TEX2D(_MainTex, float2(i.uv.x + _MainTex_TexelSize.x, i.uv.y)).xyz;

                return float4( centerCol * (1 - BlurAmount) + (topCol + botCol + leftCol + rightCol) * 0.25 * BlurAmount, 1);
            }
            ENDHLSL
        }
    }
}
