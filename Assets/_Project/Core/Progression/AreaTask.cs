namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Eine einzelne Aufgabe innerhalb einer Area. Fortschritt wird über <see cref="Book"/> gebucht;
    /// <see cref="IsComplete"/> ist abgeleitet. Buchungen auf abgeschlossene Tasks werden ignoriert.
    /// Sort-Tasks nutzen ganzzahlige Werte (Anzahl Fächer), Melt-Tasks Prozentwerte (0–100).
    /// </summary>
    public sealed class AreaTask
    {
        public AreaTask(string taskId, TaskType type, string description, float required)
        {
            TaskId      = taskId;
            Type        = type;
            Description = description;
            Required    = required > 0f ? required : 1f;
        }

        public string   TaskId      { get; }
        public TaskType Type        { get; }
        public string   Description { get; }
        public float    Required    { get; }
        public float    Current     { get; private set; }
        public bool     IsComplete  => Current >= Required;

        /// <summary>Bucht positiven Fortschritt; clamp bei Required; ignoriert wenn bereits abgeschlossen.</summary>
        public void Book(float delta)
        {
            if (IsComplete || delta <= 0f) return;
            Current = Current + delta > Required ? Required : Current + delta;
        }
    }
}
