namespace CozySanta.Core.Progression
{
    /// <summary>Unveränderliche Konfiguration einer Area: Name, Aufgaben, XP-Belohnung.</summary>
    public sealed class AreaDefinition
    {
        public AreaDefinition(string areaName, AreaTask[] tasks, int areaXp)
        {
            AreaName = areaName;
            Tasks    = tasks ?? new AreaTask[0];
            AreaXp   = areaXp > 0 ? areaXp : 0;
        }

        public string    AreaName { get; }
        public AreaTask[] Tasks   { get; }
        public int        AreaXp  { get; }
    }
}
