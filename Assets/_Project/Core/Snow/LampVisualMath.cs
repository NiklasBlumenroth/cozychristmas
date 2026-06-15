namespace CozySanta.Core.Snow
{
    /// <summary>
    /// Reine, testbare Mathematik für die Lampen-Optik (Decide). Wandelt Akku-Stand + Schmelz-Zustand in
    /// Ziel-Helligkeit und Warm/Kalt-Mischung um und glättet Werte frame-rate-unabhängig über die Zeit.
    /// Keine Unity-Typen (Core, noEngineReferences). Die Runtime (<c>LampVisuals</c>) wendet die Werte an.
    /// </summary>
    public static class LampVisualMath
    {
        private const float Tau = 6.2831853071795862f;

        /// <summary>Sanftes „Atmen" als Phase 0..1 (Sinus). Bei sin=0 ergibt sich 0.5 (Mitte).</summary>
        public static float Pulse(float time, float frequency)
        {
            return 0.5f + (0.5f * (float)System.Math.Sin(Tau * frequency * time));
        }

        /// <summary>Warm↔Kalt-Mischung 0 (leer/kalt) .. 1 (voll/warm) mit weicher Smoothstep-Kennlinie.</summary>
        public static float Warmth(float batteryFraction)
        {
            var f = Clamp01(batteryFraction);
            return f * f * (3f - (2f * f));
        }

        /// <summary>
        /// Ziel-Pegel (z. B. Emission oder Lichtstärke), skaliert mit dem Akku: aktiv geschmolzen =
        /// <paramref name="activeLevel"/>, sonst <paramref name="idleLevel"/>; leerer Akku = 0.
        /// </summary>
        public static float TargetLevel(float batteryFraction, bool active, float idleLevel, float activeLevel)
        {
            var level = active ? activeLevel : idleLevel;
            return level * Clamp01(batteryFraction);
        }

        /// <summary>Frame-rate-unabhängige exponentielle Glättung Richtung <paramref name="target"/>
        /// (überschwingt nie). <paramref name="speed"/>≤0 oder <paramref name="deltaTime"/>≤0 → unverändert.</summary>
        public static float SmoothTowards(float current, float target, float speed, float deltaTime)
        {
            if (speed <= 0f || deltaTime <= 0f)
            {
                return current;
            }

            var t = 1f - (float)System.Math.Exp(-speed * deltaTime);
            return current + ((target - current) * t);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
