Shader "CozySanta/SnowMelt"
{
    Properties
    {
        _BaseMap ("Schnee-Textur", 2D) = "white" {}
        _BaseColor ("Tönung", Color) = (1,1,1,1)
        _Tiling ("Tiling", Float) = 4
        _EdgeWidth ("Randweichheit", Range(0,0.4)) = 0.12
        _NoiseScale ("Rand-Noise", Float) = 18
        _LightDir ("Lichtrichtung", Vector) = (0.3,0.9,0.4,0)
        _Ambient ("Grundhelligkeit", Range(0,1)) = 0.5
        _Sparkle ("Glitzer", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "SnowForward"
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float  height      : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Tiling;
                float  _EdgeWidth;
                float  _NoiseScale;
                float4 _LightDir;
                float  _Ambient;
                float  _Sparkle;
            CBUFFER_END

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.height = IN.color.r; // Schneehöhe 0..1 aus dem Vertex-Color
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Weiche, unregelmäßige Schmelzkante: an freigelegten Stellen (Höhe ~0) wegclippen.
                float n = ValueNoise(IN.uv * _NoiseScale);
                float threshold = _EdgeWidth * (0.5 + n);
                clip(IN.height - threshold);

                float3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv * _Tiling).rgb * _BaseColor.rgb;

                // Einfaches Fake-Lighting (robust gegenüber Mesh-Normalenrichtung).
                float ndl = abs(dot(normalize(IN.normalWS), normalize(_LightDir.xyz)));
                float light = _Ambient + (1.0 - _Ambient) * ndl;

                // Dezenter Glitzer
                float sparkle = ValueNoise(IN.uv * _NoiseScale * 6.0);
                sparkle = saturate((sparkle - 0.85) * 6.0) * _Sparkle;

                float3 color = (albedo * light) + sparkle;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
