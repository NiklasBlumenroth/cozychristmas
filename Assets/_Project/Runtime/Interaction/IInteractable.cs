namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Vertrag eines interagierbaren Weltobjekts. Nur Objekte mit <see cref="IInteractable"/>
    /// sind Fokuskandidaten. Liegt in der Runtime-Schicht, da es konkrete Seiteneffekte beschreibt.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Kurzer Hinweistext für die Anzeige (z. B. "Aufnehmen").</summary>
        string PromptText { get; }

        /// <summary>Führt die Interaktion aus (Seiteneffekt). Konkretisiert durch F3/F4.</summary>
        void Interact();
    }
}
