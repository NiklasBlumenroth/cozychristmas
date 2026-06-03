namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Eine einzelne Skill-Option: hält die aktuelle Stufe, berechnet den abgeleiteten Wert
    /// (Basis + Schritt × Stufe, gedeckelt) und verwaltet den Freischalt-Status.
    /// </summary>
    public sealed class Skill
    {
        private readonly SkillConfig _cfg;

        public Skill(SkillId id, SkillConfig cfg)
        {
            Id   = id;
            _cfg = cfg;
        }

        public SkillId Id           { get; }
        public int     Level        { get; private set; }
        public int     MaxLevel     => _cfg.MaxLevel;
        public bool    IsUnlockable => _cfg.IsUnlockable;
        public bool    IsUnlocked   { get; private set; }
        public bool    CanRaise     => Level < MaxLevel;

        /// <summary>Abgeleiteter Wert: Basis + Schritt × Stufe, gedeckelt durch MaxValue.</summary>
        public float Value
        {
            get
            {
                var v = _cfg.BaseValue + _cfg.Step * Level;
                return v > _cfg.MaxValue ? _cfg.MaxValue : v;
            }
        }

        /// <summary>Erhöht die Stufe um 1; setzt bei Freischalt-Skills IsUnlocked. Ignoriert wenn bereits Max.</summary>
        public void Raise()
        {
            if (!CanRaise) return;
            Level++;
            if (_cfg.IsUnlockable) IsUnlocked = true;
        }
    }
}
