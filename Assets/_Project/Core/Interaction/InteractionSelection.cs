namespace CozySanta.Core.Interaction
{
    /// <summary>
    /// Ergebnis der Zielauswahl. Entweder ein gewähltes Ziel oder <see cref="None"/>.
    /// </summary>
    public readonly struct InteractionSelection
    {
        /// <summary>True, wenn ein gültiges Ziel gewählt wurde.</summary>
        public readonly bool HasTarget;

        /// <summary>Kennung des gewählten Ziels (nur gültig, wenn <see cref="HasTarget"/>).</summary>
        public readonly int TargetId;

        private InteractionSelection(bool hasTarget, int targetId)
        {
            HasTarget = hasTarget;
            TargetId = targetId;
        }

        /// <summary>Kein gültiges Ziel.</summary>
        public static InteractionSelection None => new InteractionSelection(false, 0);

        /// <summary>Erzeugt eine Auswahl für ein konkretes Ziel.</summary>
        public static InteractionSelection For(int targetId) => new InteractionSelection(true, targetId);
    }
}
