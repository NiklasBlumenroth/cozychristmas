namespace CozySanta.Core.Player
{
    /// <summary>
    /// Reine Blick-Mathematik. Keine Unity-Abhängigkeit, vollständig unit-testbar.
    /// </summary>
    public static class LookMath
    {
        /// <summary>
        /// Addiert <paramref name="deltaPitch"/> auf <paramref name="currentPitch"/> und begrenzt
        /// das Ergebnis auf [<paramref name="minPitch"/>, <paramref name="maxPitch"/>] (kein Überschlagen).
        /// </summary>
        public static float ClampPitch(float currentPitch, float deltaPitch, float minPitch, float maxPitch)
        {
            var pitch = currentPitch + deltaPitch;
            if (pitch < minPitch)
            {
                return minPitch;
            }

            if (pitch > maxPitch)
            {
                return maxPitch;
            }

            return pitch;
        }
    }
}
