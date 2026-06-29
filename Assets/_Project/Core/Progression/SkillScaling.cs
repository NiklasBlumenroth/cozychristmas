using System;

namespace CozySanta.Core.Progression
{
    /// <summary>
    /// Reine Stufen-Mathematik (Decide): interpoliert einen Skillwert linear von Start- zu Endwert über
    /// die Stufen. Funktioniert für steigende (z. B. Tragkraft 5→25) wie fallende Werte (z. B. Cooldown
    /// 10→4); die Stufe wird auf [0, maxLevel] geklemmt, das Ergebnis liegt damit stets zwischen Start und
    /// Ende. Keine UnityEngine-Abhängigkeit.
    /// </summary>
    public static class SkillScaling
    {
        /// <summary>Linear interpolierter Wert auf <paramref name="level"/> (geklemmt 0..maxLevel).</summary>
        public static float Value(float start, float end, int level, int maxLevel)
        {
            if (maxLevel <= 0) return start;
            var l = level < 0 ? 0 : (level > maxLevel ? maxLevel : level);
            var t = (float)l / maxLevel;
            return start + (end - start) * t;
        }

        /// <summary>Wie <see cref="Value"/>, kaufmännisch auf eine ganze Zahl gerundet (z. B. Ladungen).</summary>
        public static int IntValue(float start, float end, int level, int maxLevel)
            => (int)Math.Round(Value(start, end, level, maxLevel), MidpointRounding.AwayFromZero);
    }
}
