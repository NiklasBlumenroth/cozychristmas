// Prozeduraler Nachthimmel als Skybox – KEINE Texturen/Assets nötig. Alles aus der Blickrichtung
// gerechnet: Nacht-Farbverlauf, zweilagiger Sternenhimmel (mit Funkeln), Vollmond (Halo + Krater)
// und eine dezente Milchstraße. URP-tauglich (Skybox-Pass, kein SRP-Batcher nötig). Reine Optik –
// dokumentierte Nicht-Unit-Ausnahme analog zum SnowMelt-Shader.
Shader "CozySanta/NightSky"
{
    Properties
    {
        [Header(Himmel)]
        _TopColor       ("Zenit-Farbe", Color)        = (0.008, 0.015, 0.045, 1)
        _BottomColor    ("Horizont-Farbe", Color)     = (0.03, 0.05, 0.10, 1)

        [Header(Sterne)]
        _StarDensity    ("Stern-Dichte", Range(0,1))  = 0.55
        _StarBrightness ("Stern-Helligkeit", Range(0,8)) = 1.8
        _StarGlow       ("Stern-Glühen", Range(0,1))  = 0.5
        _StarSharp      ("Stern-Schärfe", Range(1,30)) = 9
        _Twinkle        ("Funkeln", Range(0,1))       = 0.6

        [Header(Mond)]
        _MoonDir        ("Mond-Richtung (xyz)", Vector) = (0.35, 0.55, -0.75, 0)
        _MoonSize       ("Mond-Größe (rad)", Range(0.005, 0.25)) = 0.055
        _MoonColor      ("Mond-Farbe", Color)         = (1, 0.97, 0.90, 1)
        _MoonGlow       ("Mond-Halo", Range(0,1))     = 0.5

        [Header(Milchstrasse)]
        _MilkyWay       ("Milchstraße", Range(0,1))   = 0.18
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _TopColor, _BottomColor, _MoonColor, _MoonDir;
            float  _StarDensity, _StarBrightness, _StarGlow, _StarSharp, _Twinkle;
            float  _MoonSize, _MoonGlow, _MilkyWay;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz; // Skybox-Mesh: Vertexposition = Richtung
                return o;
            }

            // ── Hash / Noise (texturfrei) ───────────────────────────────────────
            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float3 hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float vnoise(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash31(i + float3(0,0,0)), n100 = hash31(i + float3(1,0,0));
                float n010 = hash31(i + float3(0,1,0)), n110 = hash31(i + float3(1,1,0));
                float n001 = hash31(i + float3(0,0,1)), n101 = hash31(i + float3(1,0,1));
                float n011 = hash31(i + float3(0,1,1)), n111 = hash31(i + float3(1,1,1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float fbm(float3 p)
            {
                float s = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++) { s += a * vnoise(p); p *= 2.0; a *= 0.5; }
                return s;
            }

            // Eine Stern-Lage: pro Gitterzelle ggf. ein scharfer Lichtpunkt mit Funkeln/Helligkeitsvarianz.
            float starLayer(float3 dir, float scale, float threshold)
            {
                float3 p = dir * scale;
                float3 cell = floor(p);
                float3 fp = frac(p);
                float rnd = hash31(cell);
                if (rnd < threshold) return 0.0;

                float3 sp = hash33(cell + 7.13);
                float d = length(fp - sp);
                // Scharfer Kern + weicher, breiter Halo -> Sterne wirken leuchtend statt nur „Punkt".
                float core = pow(saturate(1.0 - d * _StarSharp), 6.0);
                float halo = pow(saturate(1.0 - d * _StarSharp * 0.28), 2.5);
                float lum = core + _StarGlow * 0.45 * halo;
                float bright = 0.35 + 0.65 * hash31(cell + 3.7);
                float tw = lerp(1.0, 0.4 + 0.6 * sin(_Time.y * 2.5 + rnd * 6.2831), _Twinkle);
                return lum * bright * tw;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);

                // Farbverlauf: oben dunkel, zum Horizont leicht aufgehellt.
                float h = saturate(dir.y);
                float3 col = lerp(_BottomColor.rgb, _TopColor.rgb, pow(h, 0.55));

                // Milchstraße (schwaches, gewölbtes Rauschband).
                float3 bandN = normalize(float3(0.25, 0.85, 0.45));
                float band = smoothstep(0.5, 0.0, abs(dot(dir, bandN)));
                float mw = fbm(dir * 7.0) * band;
                col += _MilkyWay * mw * float3(0.45, 0.5, 0.7);

                // Mond zuerst bestimmen, damit Sterne ihn nicht überlagern.
                float3 moonDir = normalize(_MoonDir.xyz);
                float md = dot(dir, moonDir);
                float ang = acos(clamp(md, -1.0, 1.0));            // Winkelabstand zur Mondmitte
                float disc = 1.0 - smoothstep(_MoonSize * 0.92, _MoonSize, ang);
                float glow = _MoonGlow * exp(-ang / (_MoonSize * 2.5));

                // Sterne (zwei Lagen: hell-spärlich + fein-dicht), außerhalb der Mondscheibe.
                float th1 = 1.0 - (0.012 + _StarDensity * 0.10);
                float th2 = 1.0 - (0.030 + _StarDensity * 0.16);
                float s = starLayer(dir, 240.0, th1) + 0.6 * starLayer(dir, 560.0, th2);
                float3 starTint = lerp(float3(0.75, 0.82, 1.0), float3(1.0, 0.93, 0.82),
                                       hash31(floor(dir * 240.0) + 1.0));
                col += s * _StarBrightness * starTint * (1.0 - disc);

                // Mondscheibe (Vollmond): leichte Krater-Modulation + Halo.
                float crater = fbm(dir * 90.0);
                float3 moonSurf = _MoonColor.rgb * (0.85 + 0.15 * crater);
                col = lerp(col, moonSurf, disc);
                col += _MoonColor.rgb * glow * (1.0 - disc);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
