using System.Collections.Generic;

namespace CozySanta.Core.Interaction
{
    /// <summary>
    /// Reine Entscheidungslogik (Decide) der Interaktions-Zielauswahl. Keine Seiteneffekte,
    /// keine Unity-Abhängigkeiten – vollständig per EditMode-/Unit-Test prüfbar.
    /// </summary>
    public static class InteractionSelector
    {
        /// <summary>
        /// Wählt aus den Kandidaten den besten gültigen Treffer:
        /// gültig sind Kandidaten mit Distanz &lt;= MaxRange UND Winkel &lt;= MaxAngle.
        /// Gewählt wird die kleinste Distanz; bei Gleichstand der kleinere Winkel.
        /// Gibt es keinen gültigen Kandidaten, wird <see cref="InteractionSelection.None"/> geliefert.
        /// </summary>
        public static InteractionSelection Decide(
            IReadOnlyList<InteractionCandidate> candidates,
            SelectionSettings settings)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return InteractionSelection.None;
            }

            var hasBest = false;
            var best = default(InteractionCandidate);

            for (var i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];

                if (c.Distance > settings.MaxRange || c.AngleToView > settings.MaxAngle)
                {
                    continue;
                }

                if (!hasBest || IsBetter(c, best))
                {
                    best = c;
                    hasBest = true;
                }
            }

            return hasBest ? InteractionSelection.For(best.TargetId) : InteractionSelection.None;
        }

        private static bool IsBetter(InteractionCandidate candidate, InteractionCandidate current)
        {
            if (candidate.Distance < current.Distance)
            {
                return true;
            }

            if (candidate.Distance > current.Distance)
            {
                return false;
            }

            // Distanzgleichstand: kleinerer Blickwinkel gewinnt.
            return candidate.AngleToView < current.AngleToView;
        }
    }
}
