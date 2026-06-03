namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Bündelt XpLedger und SkillSet. Einziger Decide-Einstieg für XP-Gutschriften und
    /// Skill-Investitionen. Verfügbare Punkte = verdient − ausgegeben (nie negativ).
    /// </summary>
    public sealed class ProgressionState
    {
        public ProgressionState(SkillConfig[] configs)
        {
            Ledger = new XpLedger();
            Skills = new SkillSet(configs);
        }

        public XpLedger Ledger          { get; }
        public SkillSet Skills          { get; }
        public int      AvailablePoints => Ledger.EarnedSkillPoints - Skills.SpentPoints;

        public void AwardXp(int amount) => Ledger.Add(amount);

        /// <summary>Investiert einen Punkt in <paramref name="id"/>. False wenn keine Punkte oder Max-Stufe.</summary>
        public bool Invest(SkillId id)  => Skills.TryInvest(id, AvailablePoints);
    }
}
