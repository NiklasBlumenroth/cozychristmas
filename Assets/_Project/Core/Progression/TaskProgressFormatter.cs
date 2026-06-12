using System.Globalization;

namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Reine, testbare Formatierung der Aufgaben-Fortschrittsanzeige (Decide); keine Unity-Abhängigkeit.
    /// Sort: <c>"12 / 96 (12,50 %)"</c> — ganzzahliger Bruch plus Prozent (2 Nachkommastellen, Komma).
    /// Melt: <c>"57 / 100 %"</c> — gerundete Prozentwerte.
    /// </summary>
    public static class TaskProgressFormatter
    {
        public static string Format(float current, float required, TaskType type)
            => type == TaskType.Melt ? FormatMelt(current, required) : FormatSort(current, required);

        /// <summary>Sort-Aufgabe: Bruch (ganzzahlig) + Prozent mit zwei Nachkommastellen (Komma).</summary>
        public static string FormatSort(float current, float required)
        {
            var percent = required > 0f ? current / required * 100f : 0f;
            var percentText = percent.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
            return $"{(int)current} / {(int)required} ({percentText} %)";
        }

        /// <summary>Melt-Aufgabe: aktueller Prozentwert mit zwei Nachkommastellen (Komma), Soll ganzzahlig.</summary>
        public static string FormatMelt(float current, float required)
        {
            var currentText = current.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
            return $"{currentText} / {(int)required} %";
        }
    }
}
