namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Gemeinsames XP-Konto. Leitet Level aus einer monoton steigenden Schwellenkurve ab
    /// und vergibt einen Skillpunkt pro Level (Platzhalterwerte, später balancierbar).
    /// </summary>
    public sealed class XpLedger
    {
        /// <summary>Kumulativer XP-Schwellenwert um Level <paramref name="level"/> zu erreichen.
        /// Kurve: level × 100 – jedes Level kostet gleichmäßig 100 XP (Platzhalter).</summary>
        public static int XpThreshold(int level) => level * 100;

        public int TotalXp         { get; private set; }
        public int Level           { get; private set; }
        public int XpIntoLevel     { get; private set; }
        public int XpForNextLevel  => XpThreshold(Level + 1) - XpThreshold(Level);

        /// <summary>Verdiente Skillpunkte: 1 pro Level.</summary>
        public int EarnedSkillPoints => Level;

        /// <summary>Bucht <paramref name="xp"/> gut. Negative/Null-Beträge werden ignoriert.</summary>
        public void Add(int xp)
        {
            if (xp <= 0) return;
            TotalXp += xp;
            RecalculateLevel();
        }

        private void RecalculateLevel()
        {
            while (TotalXp >= XpThreshold(Level + 1))
                Level++;
            XpIntoLevel = TotalXp - XpThreshold(Level);
        }
    }
}
