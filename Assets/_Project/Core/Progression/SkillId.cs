namespace CozySanta.Core.Progression
{
    /// <summary>Eindeutiger Bezeichner jeder Skill-Option. Index entspricht Array-Position in SkillSet.</summary>
    public enum SkillId
    {
        LampPower     = 0,
        LampBattery   = 1,
        CarryCapacity = 2,
        MoveSpeed     = 3,
        ObjectPull    = 4,  // Heranholen – Freischalt-Skill (Cooldown + Ladungen)
        AutoSort      = 5,  // Auto-Einsortieren – Freischalt-Skill (Cooldown + Ladungen)
    }
}
