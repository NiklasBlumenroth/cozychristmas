namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Eine einzelne Skill-Option: hält die aktuelle Stufe, berechnet den abgeleiteten Wert
    /// (linear Start→Ende über die Stufen, siehe <see cref="SkillScaling"/>) und verwaltet den
    /// Freischalt-Status. Fähigkeiten liefern zusätzlich <see cref="Charges"/> (zweite Kurve).
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

        /// <summary>Abgeleiteter Primärwert (bei Fähigkeiten: Cooldown in Sekunden).</summary>
        public float Value => SkillScaling.Value(_cfg.StartValue, _cfg.EndValue, Level, _cfg.MaxLevel);

        /// <summary>True, wenn dieser Skill eine Ladungs-Kurve hat (Fähigkeiten).</summary>
        public bool HasCharges => _cfg.HasCharges;

        /// <summary>Abgeleitete max. Ladungen (ganzzahlig), 0 wenn ohne Ladungs-Kurve.</summary>
        public int Charges => _cfg.HasCharges
            ? SkillScaling.IntValue(_cfg.ChargesStart, _cfg.ChargesEnd, Level, _cfg.MaxLevel)
            : 0;

        /// <summary>Erhöht die Stufe um 1; setzt bei Freischalt-Skills IsUnlocked. Ignoriert wenn bereits Max.</summary>
        public void Raise()
        {
            if (!CanRaise) return;
            Level++;
            if (_cfg.IsUnlockable) IsUnlocked = true;
        }
    }
}
