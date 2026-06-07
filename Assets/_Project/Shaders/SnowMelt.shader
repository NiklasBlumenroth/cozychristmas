Shader "CozySanta/SnowMelt"
{
    Properties
    {
        _BaseMap ("Schnee-Textur", 2D) = "white" {}
        _BaseColor ("Tönung", Color) = (1,1,1,1)
        _Tiling ("Tiling", Float) = 4
        _EdgeWidth ("Randweichheit", Range(0,0.4)) = 0.12
        _NoiseScale ("Rand-Noise", Float) = 18
        _LightDir ("Lichtrichtung (ungenutzt)", Vector) = (0.3,0.9,0.4,0)
        _Ambient ("Mindesthelligkeit", Range(0,1)) = 0.08
        _Sparkle ("Glitzer", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "SnowForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP-Lichtkeywords: echtes Haupt-/Zusatzlicht (Laternen) + Ambient.
            // _FORWARD_PLUS ist wichtig, damit die Zusatzlichter im Forward+-Pfad gefunden werden.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS  : TEXCOORD2;
                float  height      : TEXCOORD3;
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
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
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
                float3 normalWS = normalize(IN.normalWS);

                // Echtes Ambient aus der Szene (Spherical Harmonics) -> folgt dem Tag/Nacht-Ambiente.
                float3 lighting = SampleSH(normalWS) + _Ambient;

                // Weiche „Wrap"-Beleuchtung, damit der flache Schnee auch von tief stehenden
                // Laternen schön warm angeleuchtet wird (kein harter Terminator).
                Light mainLight = GetMainLight();
                float mainNdl = saturate(dot(normalWS, mainLight.direction) * 0.5 + 0.5);
                lighting += mainLight.color * (mainNdl * mainLight.distanceAttenuation);

                // Zusatzlichter (die Laternen): über die URP-Light-Loop-Makros, damit es im
                // Forward+- UND im klassischen Forward-Pfad funktioniert.
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light l = GetAdditionalLight(lightIndex, IN.positionWS);
                    float ndl = saturate(dot(normalWS, l.direction) * 0.5 + 0.5);
                    lighting += l.color * (ndl * l.distanceAttenuation * l.shadowAttenuation);
                LIGHT_LOOP_END

                // Glitzer nur dort, wo auch Licht ankommt (nachts im Dunkeln kein Funkeln).
                float lum = saturate(dot(lighting, float3(0.299, 0.587, 0.114)));
                float sparkle = ValueNoise(IN.uv * _NoiseScale * 6.0);
                sparkle = saturate((sparkle - 0.85) * 6.0) * _Sparkle * lum;

                float3 color = (albedo * lighting) + sparkle;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
