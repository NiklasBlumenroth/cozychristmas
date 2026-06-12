namespace CozySanta.Core.Areas
{
    /// <summary>
    /// Reine, testbare Entscheidung für die exklusive Bereichs-Aktivierung (Decide): liefert die
    /// Aktiv-Flags für N Bereiche, von denen genau einer aktiv ist. Ungültiger Index → alle inaktiv
    /// (wird in der Runtime bewusst nicht angewandt, um kein versehentliches Komplett-Abschalten
    /// auszulösen). Keine Unity-Abhängigkeit.
    /// </summary>
    public static class AreaActivation
    {
        /// <summary>Aktiv-Flags für <paramref name="count"/> Bereiche: genau <paramref name="activeIndex"/>
        /// ist true, der Rest false. Index außerhalb [0,count) → alle false.</summary>
        public static bool[] Resolve(int count, int activeIndex)
        {
            if (count < 0)
            {
                count = 0;
            }

            var flags = new bool[count];
            if (activeIndex >= 0 && activeIndex < count)
            {
                flags[activeIndex] = true;
            }

            return flags;
        }
    }
}
