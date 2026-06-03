namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Unveränderliche Konfiguration einer Skill-Option: Basiswert, Stufenschritt, Wertobergrenze,
    /// Maximalstufe und ob der Skill erst freigeschaltet werden muss.
    /// Platzhalterwerte – Balancing folgt später (Konzept 08).
    /// </summary>
    public readonly struct SkillConfig
    {
        public float BaseValue    { get; }
        public float Step         { get; }
        public float MaxValue     { get; }
        public int   MaxLevel     { get; }
        public bool  IsUnlockable { get; }

        public SkillConfig(float baseValue, float step, float maxValue, int maxLevel, bool isUnlockable = false)
        {
            BaseValue    = baseValue;
            Step         = step;
            MaxValue     = maxValue > 0f ? maxValue : float.MaxValue;
            MaxLevel     = maxLevel > 0 ? maxLevel : 1;
            IsUnlockable = isUnlockable;
        }
    }
}
