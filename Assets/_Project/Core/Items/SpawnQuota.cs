using System.Collections.Generic;

namespace CozySanta.Core.Items
{
    /// <summary>
    /// Reine Entscheidung für das gedeckelte Zufalls-Spawnen pro Bereich: aus einer Variantenliste
    /// (z. B. 96 Buch-Schlüssel) und den aktuellen Stückzahlen wird bestimmt, welche Varianten noch
    /// unter ihrer Höchstzahl liegen und welche davon als nächste gespawnt wird. Ohne UnityEngine –
    /// die Runtime liefert Zufallswert und führt das Instanziieren aus (Decide/Apply).
    /// </summary>
    public static class SpawnQuota
    {
        /// <summary>True, wenn jede Variante ihre Höchstzahl erreicht hat (kein Spawn mehr möglich).</summary>
        public static bool IsFull(
            IReadOnlyList<string> keys, IReadOnlyDictionary<string, int> counts, int maxPerVariant)
        {
            if (keys == null || keys.Count == 0) return true;

            foreach (var key in keys)
            {
                if (CountOf(counts, key) < maxPerVariant) return false;
            }

            return true;
        }

        /// <summary>
        /// Wählt aus den noch nicht vollen Varianten eine aus; <paramref name="random01"/> ∈ [0,1)
        /// bestimmt den Index in der Liste der spawnbaren Varianten. False, wenn alle voll sind.
        /// </summary>
        public static bool TryPick(
            IReadOnlyList<string> keys, IReadOnlyDictionary<string, int> counts, int maxPerVariant,
            double random01, out string key)
        {
            key = null;
            if (keys == null) return false;

            var spawnable = new List<string>();
            foreach (var k in keys)
            {
                if (CountOf(counts, k) < maxPerVariant) spawnable.Add(k);
            }

            if (spawnable.Count == 0) return false;

            var idx = (int)(random01 * spawnable.Count);
            if (idx < 0) idx = 0;
            if (idx >= spawnable.Count) idx = spawnable.Count - 1;
            key = spawnable[idx];
            return true;
        }

        private static int CountOf(IReadOnlyDictionary<string, int> counts, string key)
            => counts != null && key != null && counts.TryGetValue(key, out var c) ? c : 0;
    }
}
