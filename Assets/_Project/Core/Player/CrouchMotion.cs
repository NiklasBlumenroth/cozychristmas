namespace CozySanta.Core.Player
{
    /// <summary>
    /// Reine Hock-Mathematik (Decide). Fährt die Körperhöhe sanft zwischen Steh- und Hockhöhe an
    /// (gehaltene Hocktaste = Ziel Hockhöhe) und leitet daraus den Controller-Mittelpunkt (die Füße
    /// bleiben am Boden) sowie die Augenhöhe ab. Ohne UnityEngine – die Runtime wendet die Werte auf
    /// CharacterController + Kamera an.
    /// </summary>
    public static class CrouchMotion
    {
        /// <summary>
        /// Nächste Körperhöhe: bei gehaltener Hocktaste Richtung <paramref name="crouchHeight"/>, sonst
        /// Richtung <paramref name="standHeight"/> – sanft mit <paramref name="speed"/> (Höhe pro Sekunde)
        /// über <paramref name="deltaTime"/> angefahren, ohne Überschwingen.
        /// </summary>
        public static float StepHeight(float current, float standHeight, float crouchHeight,
            bool crouchHeld, float speed, float deltaTime)
        {
            var target = crouchHeld ? crouchHeight : standHeight;
            var maxDelta = speed * deltaTime;
            if (maxDelta < 0f) maxDelta = -maxDelta;
            return MoveTowards(current, target, maxDelta);
        }

        /// <summary>
        /// Controller-Mittelpunkt (y), sodass die Füße fix bleiben: sinkt die Höhe um Δ, sinkt der
        /// Mittelpunkt um Δ/2 – unabhängig von der ursprünglichen Mittelpunkt-Konvention.
        /// </summary>
        public static float CenterY(float standCenterY, float standHeight, float height)
            => standCenterY - ((standHeight - height) * 0.5f);

        /// <summary>Augenhöhe proportional zur aktuellen Körperhöhe (Fallback bei Steh-Höhe ≤ 0).</summary>
        public static float EyeHeight(float height, float standHeight, float standEyeHeight)
            => standHeight <= 0f ? standEyeHeight : standEyeHeight * (height / standHeight);

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            var diff = target - current;
            if (diff > maxDelta) return current + maxDelta;
            if (diff < -maxDelta) return current - maxDelta;
            return target;
        }
    }
}
