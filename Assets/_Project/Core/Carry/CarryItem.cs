namespace CozySanta.Core.Carry
{
    /// <summary>
    /// Ein getragenes Objekt als reine Daten: stabile Kennung + Gewicht. Keine Unity-Abhängigkeit.
    /// </summary>
    public readonly struct CarryItem
    {
        /// <summary>Stabile Kennung (Runtime: Instanz-Id des Pickups).</summary>
        public readonly int Id;

        /// <summary>Gewicht in kg (>= 0).</summary>
        public readonly float Weight;

        public CarryItem(int id, float weight)
        {
            Id = id;
            Weight = weight;
        }
    }
}
