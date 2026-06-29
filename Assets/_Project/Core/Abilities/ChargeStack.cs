namespace CozySanta.Core.Abilities
{
    /// <summary>
    /// Reine, testbare Ladungs-/Cooldown-Mechanik (Decide). Hält eine Anzahl Ladungen bis
    /// <see cref="MaxCharges"/>; fehlende Ladungen laden über <see cref="RechargeSeconds"/> nach
    /// (eine Ladung je Cooldown). Keine UnityEngine-Abhängigkeit; die Runtime füttert die Frame-Zeit.
    /// Genutzt von den magischen Sortierhilfen (Auto-Einsortieren, Heranholen).
    /// </summary>
    public sealed class ChargeStack
    {
        private float _recharge;
        private float _timer;

        public ChargeStack(int maxCharges, float rechargeSeconds)
        {
            MaxCharges = maxCharges < 0 ? 0 : maxCharges;
            RechargeSeconds = rechargeSeconds;
            Current = MaxCharges; // startet voll
        }

        /// <summary>Höchstzahl gleichzeitig vorrätiger Ladungen.</summary>
        public int MaxCharges { get; private set; }

        /// <summary>Aufladezeit je fehlender Ladung (Sekunden, mindestens ein Minimalwert).</summary>
        public float RechargeSeconds
        {
            get => _recharge;
            private set => _recharge = value < 0.0001f ? 0.0001f : value;
        }

        /// <summary>Aktuell verfügbare Ladungen.</summary>
        public int Current { get; private set; }

        /// <summary>True, solange mindestens eine Ladung verfügbar ist.</summary>
        public bool HasCharge => Current > 0;

        /// <summary>Fortschritt zur nächsten Ladung 0..1 (1, wenn bereits voll).</summary>
        public float Fraction => Current >= MaxCharges ? 1f : Clamp01(_timer / _recharge);

        /// <summary>Treibt den Cooldown um <paramref name="deltaSeconds"/>; füllt fehlende Ladungen nach.</summary>
        public void Step(float deltaSeconds)
        {
            if (Current >= MaxCharges)
            {
                _timer = 0f;
                return;
            }

            if (deltaSeconds <= 0f) return;

            _timer += deltaSeconds;
            while (_timer >= _recharge && Current < MaxCharges)
            {
                _timer -= _recharge;
                Current++;
            }

            if (Current >= MaxCharges) _timer = 0f;
        }

        /// <summary>Zieht eine Ladung ab. False, wenn keine verfügbar ist.</summary>
        public bool TryConsume()
        {
            if (Current <= 0) return false;
            Current--;
            return true;
        }

        /// <summary>Aktualisiert Parameter (z. B. nach Skill-Investition). Current wird auf das neue Max geklemmt.</summary>
        public void Configure(int maxCharges, float rechargeSeconds)
        {
            MaxCharges = maxCharges < 0 ? 0 : maxCharges;
            RechargeSeconds = rechargeSeconds;
            if (Current > MaxCharges) Current = MaxCharges;
        }

        /// <summary>Setzt die Ladungen sofort auf das Maximum (z. B. beim Freischalten).</summary>
        public void Refill()
        {
            Current = MaxCharges;
            _timer = 0f;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
