using System.Collections.Generic;

namespace CozySanta.Core.Sorting
{
    /// <summary>
    /// Reine, testbare Sortier-Fach-Logik (Decide): klassifiziert eingelegte Objekte gegen den
    /// akzeptierten <see cref="SortKey"/>, hält die LIFO-Reihenfolge der Einlagen, bewertet
    /// Vollständigkeit gegen eine Soll-Menge und sperrt das Fach beim Abschluss. Keine
    /// Unity-Abhängigkeit; Seiteneffekte (Reparenting, Lampe, Schließen) liegen in der Runtime.
    /// </summary>
    public sealed class SortTarget
    {
        private readonly struct Entry
        {
            public readonly int Id;
            public readonly bool Correct;

            public Entry(int id, bool correct)
            {
                Id = id;
                Correct = correct;
            }
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public SortTarget(SortKey accepted, int requiredCount)
        {
            Accepted = accepted;
            RequiredCount = requiredCount < 0 ? 0 : requiredCount;
        }

        /// <summary>Akzeptierte Kategorie dieses Fachs.</summary>
        public SortKey Accepted { get; }

        /// <summary>Soll-Menge korrekter Objekte für den Abschluss.</summary>
        public int RequiredCount { get; }

        /// <summary>Anzahl korrekt klassifizierter Einlagen.</summary>
        public int CorrectCount { get; private set; }

        /// <summary>Anzahl falsch klassifizierter Einlagen.</summary>
        public int WrongCount { get; private set; }

        /// <summary>Gesamtzahl eingelegter Objekte.</summary>
        public int Count => _entries.Count;

        /// <summary>Decide: Hat das Fach noch einen freien Platz (offen und unter der Soll-Menge)?
        /// Andockpunkt für die Ghost-Vorschau ("valider, nicht voller Slot").</summary>
        public bool HasFreeSlot => !IsClosed && _entries.Count < RequiredCount;

        /// <summary>True, sobald das Fach einmal vollständig war (danach gesperrt).</summary>
        public bool IsClosed { get; private set; }

        /// <summary>True nur in dem Schritt, in dem der Abschluss gerade erreicht wurde.</summary>
        public bool JustCompleted { get; private set; }

        /// <summary>Decide: passt <paramref name="itemKey"/> zur akzeptierten Kategorie (korrekt)?</summary>
        public bool Classify(SortKey itemKey) => itemKey.Matches(Accepted);

        /// <summary>
        /// Legt ein Objekt oben auf (LIFO) und klassifiziert es. False, wenn das Fach bereits
        /// abgeschlossen/gesperrt ist. Setzt <see cref="JustCompleted"/>, wenn der Abschluss dadurch
        /// gerade erreicht wird.
        /// </summary>
        public bool TryPlace(int id, SortKey itemKey)
        {
            JustCompleted = false;
            if (IsClosed)
            {
                return false;
            }

            var correct = Classify(itemKey);
            _entries.Add(new Entry(id, correct));
            if (correct)
            {
                CorrectCount++;
            }
            else
            {
                WrongCount++;
            }

            UpdateCompletion();
            return true;
        }

        /// <summary>Liest die Id des zuletzt eingelegten Objekts (LIFO) ohne Entnahme.</summary>
        public bool TryPeekTop(out int id)
        {
            if (IsClosed || _entries.Count == 0)
            {
                id = 0;
                return false;
            }

            id = _entries[_entries.Count - 1].Id;
            return true;
        }

        /// <summary>
        /// Entnimmt das zuletzt eingelegte Objekt (LIFO) und liefert dessen Id. False bei leerem oder
        /// abgeschlossenem Fach.
        /// </summary>
        public bool TryRemoveTop(out int id)
        {
            JustCompleted = false;
            if (IsClosed || _entries.Count == 0)
            {
                id = 0;
                return false;
            }

            var last = _entries.Count - 1;
            var entry = _entries[last];
            _entries.RemoveAt(last);
            if (entry.Correct)
            {
                CorrectCount--;
            }
            else
            {
                WrongCount--;
            }

            id = entry.Id;
            return true;
        }

        /// <summary>Decide (rein): leitet den fachlichen Zustand aus den Zählständen ab.</summary>
        public SortTargetState Evaluate()
        {
            if (CorrectCount >= RequiredCount && WrongCount == 0 && RequiredCount > 0)
            {
                return SortTargetState.Vollstaendig;
            }

            if (WrongCount > 0)
            {
                return SortTargetState.FalschEnthalten;
            }

            return CorrectCount == 0 ? SortTargetState.Leer : SortTargetState.Teilweise;
        }

        private void UpdateCompletion()
        {
            if (CorrectCount >= RequiredCount && WrongCount == 0 && RequiredCount > 0)
            {
                IsClosed = true;
                JustCompleted = true;
            }
        }
    }
}
