using System.Collections.Generic;

namespace CozySanta.Core.Interaction
{
    /// <summary>
    /// Provider-Abstraktion für die Weltabfrage: liefert die aktuell sichtbaren/erreichbaren
    /// Interaktionskandidaten. Die Runtime implementiert dies über Unity-Physics; Tests injizieren
    /// einen Fake. So bleibt die Entscheidungslogik (Decide) frei von Unity-Abhängigkeiten.
    /// </summary>
    public interface IInteractionProbe
    {
        IReadOnlyList<InteractionCandidate> QueryCandidates();
    }
}
