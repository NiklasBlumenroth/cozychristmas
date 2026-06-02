namespace CozySanta.Core.Snow
{
    /// <summary>
    /// Akku der Schmelzlampe (reine Logik, ohne Unity). Verbraucht beim Schmelzen, sperrt bei leer,
    /// lädt wieder auf. Parameter sind Andockpunkt für spätere Akku-Upgrades (F6).
    /// </summary>
    public sealed class LampBattery
    {
        public LampBattery(float capacity)
        {
            Capacity = capacity < 0f ? 0f : capacity;
            Level = Capacity;
        }

        /// <summary>Maximale Kapazität (setzbar für Upgrades).</summary>
        public float Capacity { get; set; }

        /// <summary>Aktueller Ladestand (0..Capacity).</summary>
        public float Level { get; private set; }

        /// <summary>True, wenn noch Energie zum Schmelzen da ist (Stand > 0).</summary>
        public bool CanMelt => Level > 0f;

        /// <summary>Ladestand als Anteil 0..1 (für Anzeige).</summary>
        public float Fraction => Capacity > 0f ? Level / Capacity : 0f;

        /// <summary>Verbraucht Energie (clamp bei 0).</summary>
        public void Drain(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            Level -= amount;
            if (Level < 0f)
            {
                Level = 0f;
            }
        }

        /// <summary>Lädt Energie nach (clamp bei Capacity).</summary>
        public void Recharge(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            Level += amount;
            if (Level > Capacity)
            {
                Level = Capacity;
            }
        }

        /// <summary>Setzt den Akku auf volle Kapazität.</summary>
        public void Refill() => Level = Capacity;
    }
}
