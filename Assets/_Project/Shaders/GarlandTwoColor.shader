// Girlande mit ZWEI Lichtfarben in EINEM Material: die Emission der Birnchen (Maske aus _EmissionMap)
// wird entlang einer Objektraum-Achse abwechselnd in Farbe A (Rot) und B (Grün) eingefärbt. Im Objektraum
// sind die Birnchen gleichmäßig verteilt – über _Axis + _Frequency lässt sich der Wechsel auf „pro Birnchen"
// einstellen. Unlit (URP rendert die Pass als SRPDefaultUnlit) – für ein leuchtendes Deko-Objekt ausreichend;
// reine Optik, keine Fachlogik. Bloom (URP) lässt die HDR-Farben glühen.
Shader "CozySanta/GarlandTwoColor"
{
    Properties
    {
        _BaseMap      ("Base Map (Albedo)", 2D) = "white" {}
        _BaseTint     ("Basis-Tönung", Color)   = (1, 1, 1, 1)
        _EmissionMap  ("Emission-Maske", 2D)    = "white" {}
        [HDR]_ColorA  ("Licht A (Rot)", Color)  = (3.0, 0.10, 0.10, 1)
        [HDR]_ColorB  ("Licht B (Grün)", Color) = (0.10, 2.6, 0.20, 1)
        _Axis         ("Wechsel-Achse (Objektraum)", Vector) = (0, 1, 0, 0)
        _Frequency    ("Wechsel-Frequenz", Float) = 8
        _Phase        ("Phase", Range(0,1))     = 0
        _Softness     ("Übergangs-Weichheit", Range(0,0.5)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;     float4 _BaseMap_ST;
            sampler2D _EmissionMap;
            float4 _ColorA, _ColorB, _Axis, _BaseTint;
            float  _Frequency, _Phase, _Softness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float along : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _BaseMap);
                // Lage entlang der gewählten Objektraum-Achse -> bestimmt rot/grün.
                float3 ax = normalize(_Axis.xyz + float3(1e-5, 0, 0));
                o.along = dot(v.vertex.xyz, ax);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 baseC = tex2D(_BaseMap, i.uv).rgb * _BaseTint.rgb;
                fixed  mask  = tex2D(_EmissionMap, i.uv).r;

                // Sägezahn 0..1 entlang der Achse; weicher Wechsel A<->B.
                float t = frac(i.along * _Frequency + _Phase);
                float pick = smoothstep(0.5 - _Softness, 0.5 + _Softness, t);
                fixed3 lightCol = lerp(_ColorA.rgb, _ColorB.rgb, pick);

                // An den Birnchen (mask=1) die helle Albedo durch die Lichtfarbe ERSETZEN, nicht
                // addieren – sonst clamped Weiß + HDR-Farbe wieder auf Weiß.
                return fixed4(lerp(baseC, lightCol, mask), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
