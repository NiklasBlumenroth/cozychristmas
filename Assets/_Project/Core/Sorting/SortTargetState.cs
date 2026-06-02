namespace CozySanta.Core.Sorting
{
    /// <summary>
    /// Lebenszyklus-Zustand eines Sortier-Fachs (rein fachlich, ohne Unity).
    /// </summary>
    public enum SortTargetState
    {
        /// <summary>Keine Objekte eingelegt.</summary>
        Leer,

        /// <summary>Korrekte Objekte vorhanden, Soll-Menge noch nicht erreicht; keine falschen.</summary>
        Teilweise,

        /// <summary>Mindestens ein falsch klassifiziertes Objekt eingelegt (Lampe bleibt aus).</summary>
        FalschEnthalten,

        /// <summary>Soll-Menge korrekter Objekte erreicht und 0 falsche; abgeschlossen/gesperrt.</summary>
        Vollstaendig
    }
}
