namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Unveränderliche Konfiguration einer Skill-Option, datengetrieben aus der editierbaren
    /// Stufen-Tabelle (<c>SkillTableConfig</c>): primärer Wert läuft linear von
    /// <see cref="StartValue"/> zu <see cref="EndValue"/> über <see cref="MaxLevel"/> Stufen.
    /// Fähigkeiten (Auto-Einsortieren, Heranholen) haben zusätzlich eine zweite Kurve für die
    /// Ladungen (<see cref="ChargesStart"/> → <see cref="ChargesEnd"/>); ihr Primärwert ist der
    /// Cooldown. Platzhalterwerte – Balancing über das Asset.
    /// </summary>
    public readonly struct SkillConfig
    {
        public float StartValue   { get; }
        public float EndValue     { get; }
        public int   MaxLevel     { get; }
        public bool  IsUnlockable { get; }

        /// <summary>True, wenn der Skill eine zweite (Ladungs-)Kurve hat (Fähigkeiten).</summary>
        public bool  HasCharges   { get; }
        public float ChargesStart { get; }
        public float ChargesEnd   { get; }

        public SkillConfig(float startValue, float endValue, int maxLevel, bool isUnlockable = false,
            bool hasCharges = false, float chargesStart = 0f, float chargesEnd = 0f)
        {
            StartValue   = startValue;
            EndValue     = endValue;
            MaxLevel     = maxLevel > 0 ? maxLevel : 1;
            IsUnlockable = isUnlockable;
            HasCharges   = hasCharges;
            ChargesStart = chargesStart;
            ChargesEnd   = chargesEnd;
        }
    }
}
