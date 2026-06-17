using CozySanta.Core.Snow;
using UnityEngine;

namespace CozySanta.Runtime.Snow
{
    /// <summary>
    /// Lampen-Optik (Apply): spiegelt Akku-Stand + Schmelz-Zustand des <see cref="MeltController"/> auf die
    /// Emission des Funke-Renderers, die Lichtstärke (Kern-/Kegel-Licht) und die Funkel-Partikel. Die Mathe
    /// (Warm/Kalt, Ziel-Pegel, Glättung, Atmen) liegt testbar in <see cref="LampVisualMath"/>; hier nur die
    /// Seiteneffekte. Voll = warmes Gold + hell, leer = kühles Blau + matt; beim Schmelzen heller + mehr Funken.
    /// </summary>
    public sealed class LampVisuals : MonoBehaviour
    {
        [SerializeField] private MeltController lamp;

        [Header("Helligkeit (Master-Regler)")]
        [Tooltip("Gemeinsamer Multiplikator für Emission UND Lichtstärke – passiv wie aktiv. " +
                 "Hochziehen macht die Lampe insgesamt heller (live im Play-Mode änderbar). 1 = Grundwert.")]
        [Range(0.1f, 8f)]
        [SerializeField] private float brightness = 1f;

        [Header("Funke (Emission)")]
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private Color warmColor = new Color(1f, 0.70f, 0.30f);
        [SerializeField] private Color coldColor = new Color(0.35f, 0.55f, 1f);
        [SerializeField] private float idleEmission = 2.5f;
        [SerializeField] private float activeEmission = 6f;
        [Tooltip("Minimal-Glühen auch bei leerem Akku, damit das kühle Blau sichtbar bleibt (statt komplett aus).")]
        [SerializeField] private float coldFloor = 0.6f;

        [Header("Atmen (Puls)")]
        [SerializeField] private float pulseFrequency = 0.7f;
        [Range(0f, 0.5f)]
        [SerializeField] private float pulseDepth = 0.12f;

        [Header("Lichter")]
        [SerializeField] private Light coreLight;
        [SerializeField] private Light coneLight;
        [SerializeField] private float idleLightIntensity = 1.2f;
        [SerializeField] private float activeLightIntensity = 3f;

        [Header("Funkel-Partikel")]
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private float idleSparkRate = 4f;
        [SerializeField] private float activeSparkRate = 20f;

        [Header("Glättung")]
        [Tooltip("Höher = reagiert schneller. Lerp ist frame-rate-unabhängig (nie hartes Umschalten).")]
        [SerializeField] private float smoothSpeed = 6f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;
        private float _glow;
        private float _light;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _glow = idleEmission;
            _light = idleLightIntensity;
        }

        private void LateUpdate()
        {
            if (lamp == null)
            {
                return;
            }

            var dt = UnityEngine.Time.deltaTime;
            var frac = lamp.BatteryFraction;
            var melting = lamp.IsMelting;

            var color = Color.Lerp(coldColor, warmColor, LampVisualMath.Warmth(frac));
            var breathing = Mathf.Lerp(1f - pulseDepth, 1f + pulseDepth,
                LampVisualMath.Pulse(UnityEngine.Time.time, pulseFrequency));

            var glowTarget = Mathf.Max(coldFloor,
                LampVisualMath.TargetLevel(frac, melting, idleEmission, activeEmission)) * brightness;
            _glow = LampVisualMath.SmoothTowards(_glow, glowTarget, smoothSpeed, dt);

            if (orbRenderer != null)
            {
                orbRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, color * (_glow * breathing));
                orbRenderer.SetPropertyBlock(_mpb);
            }

            var lightTarget = Mathf.Max(coldFloor * 0.5f,
                LampVisualMath.TargetLevel(frac, melting, idleLightIntensity, activeLightIntensity)) * brightness;
            _light = LampVisualMath.SmoothTowards(_light, lightTarget, smoothSpeed, dt);

            if (coreLight != null)
            {
                coreLight.color = color;
                coreLight.intensity = _light * breathing;
            }

            if (coneLight != null)
            {
                coneLight.color = color;
                coneLight.intensity = _light * breathing * (melting ? 1f : 0.4f);
            }

            if (sparks != null)
            {
                var emission = sparks.emission;
                emission.rateOverTime = melting ? activeSparkRate : idleSparkRate * frac;
            }
        }
    }
}
