using System.Collections.Generic;

namespace CozySanta.Core.Rendering
{
    /// <summary>
    /// Reine, dichte Slot-Verwaltung für instanziertes Rendern (Decide): hält je Render-Gruppe die
    /// Besitzer-Ids (Item-InstanceID) in einem lückenlosen Array. Die Runtime führt dazu parallele
    /// <c>Matrix4x4[]</c>/<c>Transform[]</c> und spiegelt jede Mutation über die zurückgegebene
    /// Verschiebe-Info – so bleibt alles ohne UnityEngine-Typen testbar (Matrizen liegen in der Runtime).
    ///
    /// Entfernen nutzt <b>Swap-with-last</b>: Das letzte Element rückt auf den frei gewordenen Index,
    /// das Array bleibt dicht (kein Lücken-Kompaktieren, O(1) statt O(n)). <see cref="RemoveAt"/> meldet
    /// den Quell-Index des nachgerückten Elements, damit die Runtime ihre Parallel-Arrays gleich umkopiert.
    /// </summary>
    public sealed class InstanceSlots
    {
        private readonly List<int> _owners = new List<int>();

        /// <summary>Anzahl belegter Slots.</summary>
        public int Count => _owners.Count;

        /// <summary>Besitzer-Id am Slot-Index (für das Suchen aller Slots eines Items).</summary>
        public int OwnerAt(int index) => _owners[index];

        /// <summary>Hängt einen Slot für <paramref name="ownerId"/> an und liefert seinen Index.</summary>
        public int Add(int ownerId)
        {
            _owners.Add(ownerId);
            return _owners.Count - 1;
        }

        /// <summary>
        /// Entfernt den Slot an <paramref name="index"/> per Swap-with-last. Rückgabe: der Quell-Index des
        /// nachgerückten Elements (== bisheriger letzter Index), oder <c>-1</c>, wenn der entfernte Slot
        /// bereits der letzte war (dann rückt nichts nach). Bei ungültigem Index <c>-1</c> ohne Änderung.
        /// Die Runtime kopiert dann <c>arr[index] = arr[rückgabe]</c> (falls ≥ 0) und kürzt um 1.
        /// </summary>
        public int RemoveAt(int index)
        {
            var last = _owners.Count - 1;
            if (index < 0 || index > last)
            {
                return -1;
            }

            if (index != last)
            {
                _owners[index] = _owners[last];
            }

            _owners.RemoveAt(last);
            return index != last ? last : -1;
        }

        /// <summary>Leert alle Slots (z. B. beim Zurücksetzen einer Gruppe).</summary>
        public void Clear() => _owners.Clear();

        /// <summary>
        /// Zerlegt <paramref name="total"/> Slots in aufeinanderfolgende Bereiche von höchstens
        /// <paramref name="maxPerBatch"/> Instanzen (Grenze von <c>Graphics.RenderMeshInstanced</c> = 1023).
        /// Liefert je Bereich (Start-Index, Anzahl); bei <paramref name="total"/> 0 keine Bereiche.
        /// </summary>
        public static IEnumerable<(int start, int count)> ChunkRanges(int total, int maxPerBatch)
        {
            if (maxPerBatch < 1)
            {
                maxPerBatch = 1;
            }

            for (var start = 0; start < total; start += maxPerBatch)
            {
                var count = total - start;
                if (count > maxPerBatch)
                {
                    count = maxPerBatch;
                }

                yield return (start, count);
            }
        }
    }
}
