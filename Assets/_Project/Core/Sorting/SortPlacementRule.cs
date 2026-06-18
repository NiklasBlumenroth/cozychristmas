using System;
using System.Collections.Generic;

namespace CozySanta.Core.Sorting
{
    /// <summary>
    /// Reine Entscheidung (Decide), ob ein Item in ein Fach EINGELEGT werden darf – eine grobe Sperre nach
    /// Art (erster Facette), getrennt von der korrekt/falsch-Bewertung in <see cref="SortTarget"/>. Eine
    /// leere/fehlende Erlaubnisliste bedeutet „jedes Item einlegbar" (bisheriges Standardverhalten), sodass
    /// nur Fächer mit gesetzter Liste eingeschränkt werden. Ohne UnityEngine.
    /// </summary>
    public static class SortPlacementRule
    {
        /// <summary>
        /// True, wenn das Item eingelegt werden darf: keine Liste/leer = immer erlaubt; sonst muss die
        /// erste Facette (Art) des <paramref name="itemKey"/> in <paramref name="allowedArts"/> enthalten
        /// sein. Items ohne Art (leerer Key) sind bei gesetzter Liste nicht erlaubt.
        /// </summary>
        public static bool IsPlaceable(SortKey itemKey, IReadOnlyList<string> allowedArts)
        {
            if (allowedArts == null || allowedArts.Count == 0)
            {
                return true;
            }

            var art = itemKey.Facet(0);
            if (art == null)
            {
                return false;
            }

            for (var i = 0; i < allowedArts.Count; i++)
            {
                if (string.Equals(allowedArts[i], art, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
