using System.Collections.Generic;

namespace CozySanta.Core.Sorting
{
    /// <summary>
    /// Reine, testbare Validierungslogik der Geschenk-Truhe (Decide). Beim Schließen prüft die Truhe
    /// ihren Inhalt gegen genau EINEN akzeptierten <see cref="SortKey"/> nach dem Prinzip
    /// „alles-oder-nichts": Sind ALLE enthaltenen Items die richtige Sorte, werden sie angenommen
    /// (verschwinden) und auf den kumulierten Zählstand gebucht; ist auch nur eines falsch, wird der
    /// GESAMTE Inhalt abgewiesen (fliegt durch die Röhre zurück). Erreicht der Zählstand die
    /// Soll-Menge, verriegelt die Truhe. Keine Unity-Abhängigkeit; Seiteneffekte (Deckel, Zerstören,
    /// Auswerfen) liegen in der Runtime.
    /// </summary>
    public static class GiftChestValidation
    {
        /// <summary>Ergebnis einer Schließ-Validierung.</summary>
        public readonly struct Decision
        {
            public Decision(bool allCorrect, int acceptCount, bool locks)
            {
                AllCorrect = allCorrect;
                AcceptCount = acceptCount;
                Locks = locks;
            }

            /// <summary>True, wenn der gesamte Inhalt die akzeptierte Sorte ist (leerer Inhalt = true).</summary>
            public bool AllCorrect { get; }

            /// <summary>Anzahl anzunehmender (zu zerstörender) Items. 0, wenn abgewiesen wird.</summary>
            public int AcceptCount { get; }

            /// <summary>True, wenn die Truhe durch diese Annahme die Soll-Menge erreicht und verriegelt.</summary>
            public bool Locks { get; }

            /// <summary>True, wenn der Inhalt abgewiesen wird (mind. ein falsches Item) – Runtime wirft alles aus.</summary>
            public bool Reject => !AllCorrect;
        }

        /// <summary>
        /// Entscheidet über einen Schließvorgang. <paramref name="contained"/> sind die SortKeys der
        /// aktuell in der Truhe liegenden Items, <paramref name="accepted"/> die akzeptierte Sorte,
        /// <paramref name="alreadyAccepted"/> der bisher gebuchte Zählstand und <paramref name="required"/>
        /// die Soll-Menge zum Verriegeln. Leerer Inhalt ist „korrekt", nimmt aber nichts an.
        /// </summary>
        public static Decision Decide(IReadOnlyList<SortKey> contained, SortKey accepted,
                                      int alreadyAccepted, int required)
        {
            var count = contained?.Count ?? 0;
            if (count == 0)
            {
                return new Decision(allCorrect: true, acceptCount: 0, locks: false);
            }

            for (var i = 0; i < count; i++)
            {
                if (!contained[i].Matches(accepted))
                {
                    // Alles-oder-nichts: ein falsches Item weist den gesamten Inhalt ab.
                    return new Decision(allCorrect: false, acceptCount: 0, locks: false);
                }
            }

            var locks = required > 0 && alreadyAccepted + count >= required;
            return new Decision(allCorrect: true, acceptCount: count, locks: locks);
        }
    }
}
