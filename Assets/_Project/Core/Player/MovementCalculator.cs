using System.Numerics;

namespace CozySanta.Core.Player
{
    /// <summary>
    /// Reine Bewegungs-Mathematik. Liefert die lokale Planar-Geschwindigkeit (Strafe/Vorwärts)
    /// ohne Schwerkraft und ohne deltaTime; die Runtime mappt auf Welt-Achsen und wendet sie an.
    /// </summary>
    public static class MovementCalculator
    {
        /// <summary>
        /// <paramref name="input"/>: X = Strafe, Y = Vorwärts. Diagonale wird auf Betrag 1 normiert
        /// (nicht schneller als geradeaus), anschließend mit <paramref name="speed"/> skaliert.
        /// </summary>
        public static Vector2 ComputeLocalVelocity(Vector2 input, float speed)
        {
            var length = input.Length();
            if (length <= 0f)
            {
                return Vector2.Zero;
            }

            var direction = length > 1f ? input / length : input;
            return direction * speed;
        }
    }
}
