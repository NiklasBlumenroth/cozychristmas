namespace CozySanta.Core.Sorting
{
    /// <summary>
    /// Reine, testbare Reihenfolge-Logik für den „Container"-Füllmodus: bestimmt innerhalb EINER
    /// x-Spalte (die per Zielen gewählt wird) die nächste einzulegende bzw. zu entnehmende Zelle.
    /// Füllreihenfolge je Spalte: unten→oben (y), hinten→vorne (z); Entnahme spiegelverkehrt
    /// (oben→unten, vorne→hinten). x bleibt fix. Keine Unity-Abhängigkeit.
    /// </summary>
    public static class SlotFillOrder
    {
        /// <summary>Nächste FREIE Zelle in Spalte <paramref name="x"/> (unten→oben, hinten→vorne).
        /// False, wenn die Spalte voll ist.</summary>
        public static bool TryNextFree(bool[,,] occupied, int x, out int y, out int z)
        {
            int sy = occupied.GetLength(1), sz = occupied.GetLength(2);
            for (var yy = 0; yy < sy; yy++)
            for (var zz = sz - 1; zz >= 0; zz--)
            {
                if (!occupied[x, yy, zz])
                {
                    y = yy; z = zz;
                    return true;
                }
            }

            y = z = -1;
            return false;
        }

        /// <summary>Nächste BELEGTE Zelle in Spalte <paramref name="x"/> (oben→unten, vorne→hinten).
        /// False, wenn die Spalte leer ist.</summary>
        public static bool TryNextOccupied(bool[,,] occupied, int x, out int y, out int z)
        {
            int sy = occupied.GetLength(1), sz = occupied.GetLength(2);
            for (var yy = sy - 1; yy >= 0; yy--)
            for (var zz = 0; zz < sz; zz++)
            {
                if (occupied[x, yy, zz])
                {
                    y = yy; z = zz;
                    return true;
                }
            }

            y = z = -1;
            return false;
        }
    }
}
