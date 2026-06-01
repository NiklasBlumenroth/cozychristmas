namespace CozySanta.Core.Interaction
{
    /// <summary>
    /// Ein möglicher Interaktionskandidat aus der Weltabfrage. Reine Daten, keine Unity-Typen.
    /// </summary>
    public readonly struct InteractionCandidate
    {
        /// <summary>Stabile Kennung des Kandidaten (z. B. Instanz-Id eines Weltobjekts).</summary>
        public readonly int TargetId;

        /// <summary>Distanz zum Spieler/Blickursprung in Metern (>= 0).</summary>
        public readonly float Distance;

        /// <summary>Winkel zwischen Blickrichtung und Kandidat in Grad (>= 0).</summary>
        public readonly float AngleToView;

        public InteractionCandidate(int targetId, float distance, float angleToView)
        {
            TargetId = targetId;
            Distance = distance;
            AngleToView = angleToView;
        }
    }
}
