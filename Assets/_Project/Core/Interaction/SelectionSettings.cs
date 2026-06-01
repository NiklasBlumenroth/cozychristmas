namespace CozySanta.Core.Interaction
{
    /// <summary>
    /// Parameter der Zielauswahl: maximale Reichweite und maximaler Blickwinkel.
    /// </summary>
    public readonly struct SelectionSettings
    {
        /// <summary>Maximale Interaktionsdistanz in Metern (> 0).</summary>
        public readonly float MaxRange;

        /// <summary>Maximaler Blickwinkel in Grad (> 0).</summary>
        public readonly float MaxAngle;

        public SelectionSettings(float maxRange, float maxAngle)
        {
            MaxRange = maxRange;
            MaxAngle = maxAngle;
        }
    }
}
