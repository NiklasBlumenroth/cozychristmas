namespace CozySanta.Core.Player
{
    /// <summary>
    /// Reine Vertikal-/Sprung-Mathematik. Liefert die Absprunggeschwindigkeit aus gewünschter
    /// Sprunghöhe und Schwerkraft und schreibt die Vertikalgeschwindigkeit pro Frame fort
    /// (Schwerkraft-Integration, Boden-Anpressdruck, Absprung). Ohne deltaTime-Seiteneffekte –
    /// die Runtime wendet das Ergebnis auf den <c>CharacterController</c> an.
    /// </summary>
    public static class JumpCalculator
    {
        /// <summary>
        /// Absprunggeschwindigkeit, sodass der Scheitelpunkt <paramref name="jumpHeight"/> erreicht:
        /// v0 = sqrt(2 · |g| · h). Bei nicht-positiver Höhe oder Schwerkraft = 0.
        /// </summary>
        public static float ComputeJumpVelocity(float jumpHeight, float gravity)
        {
            var g = gravity < 0f ? -gravity : gravity;
            if (jumpHeight <= 0f || g <= 0f)
            {
                return 0f;
            }

            return (float)System.Math.Sqrt(2.0 * g * jumpHeight);
        }

        /// <summary>
        /// Decide: nächste Vertikalgeschwindigkeit. Am Boden wird ein sanfter Anpressdruck (-2)
        /// gehalten; liegt zugleich eine Sprunganforderung vor, ersetzt <paramref name="jumpVelocity"/>
        /// die Geschwindigkeit. Sonst integriert die Schwerkraft über <paramref name="deltaTime"/>.
        /// </summary>
        public static float StepVerticalVelocity(
            float current, bool grounded, bool jumpRequested,
            float jumpVelocity, float gravity, float deltaTime)
        {
            if (grounded && current < 0f)
            {
                current = -2f;
            }

            if (grounded && jumpRequested)
            {
                return jumpVelocity;
            }

            return current + (gravity * deltaTime);
        }
    }
}
