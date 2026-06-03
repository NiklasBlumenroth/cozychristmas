namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Sammlung aller Skill-Optionen. Prüft Investitionsbedingungen (Punkte + Maximalstufe)
    /// und führt Investitionen durch. Index der internen Skills entspricht SkillId-Enum-Wert.
    /// </summary>
    public sealed class SkillSet
    {
        private readonly Skill[] _skills;

        public SkillSet(SkillConfig[] configs)
        {
            _skills = new Skill[configs.Length];
            for (var i = 0; i < configs.Length; i++)
                _skills[i] = new Skill((SkillId)i, configs[i]);
        }

        /// <summary>Anzahl bisher ausgegebener Skillpunkte.</summary>
        public int SpentPoints { get; private set; }

        public Skill Get(SkillId id)    => _skills[(int)id];
        public float ValueOf(SkillId id) => Get(id).Value;

        public bool CanInvest(SkillId id, int availablePoints)
            => availablePoints > 0 && Get(id).CanRaise;

        /// <summary>
        /// Investiert einen Punkt in <paramref name="id"/>. Gibt true zurück wenn erfolgreich,
        /// false wenn keine Punkte verfügbar oder Skill bereits auf Maximalstufe.
        /// </summary>
        public bool TryInvest(SkillId id, int availablePoints)
        {
            if (!CanInvest(id, availablePoints)) return false;
            Get(id).Raise();
            SpentPoints++;
            return true;
        }
    }
}
