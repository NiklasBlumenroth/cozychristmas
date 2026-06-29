using CozySanta.Core.Abilities;
using CozySanta.Core.Sorting;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Sorting;
using UnityEngine;

namespace CozySanta.Runtime.Abilities
{
    /// <summary>
    /// Gemeinsame Basis der magischen Sortierhilfen (Apply-Schicht zur Core-<see cref="ChargeStack"/>):
    /// hält Ladungen/Cooldown, den Freischalt-Status und die geteilten Abfragen (getragenes Objekt,
    /// aktuelles Gebäude). Liegt auf dem Spieler-Objekt; <see cref="PlayerProgression"/> ruft
    /// <see cref="Configure"/> nach jeder Skill-Investition. Konkrete Wirkung in den Unterklassen.
    /// </summary>
    public abstract class MagicAbility : MonoBehaviour
    {
        [SerializeField] protected PlayerCarry carry;

        private readonly ChargeStack _charges = new ChargeStack(0, 1f);
        private bool _unlocked;

        /// <summary>Aktuell verfügbare Ladungen (für HUD/Diagnose).</summary>
        public int CurrentCharges => _charges.Current;
        public int MaxCharges => _charges.MaxCharges;
        public bool IsUnlocked => _unlocked;
        protected ChargeStack Charges => _charges;

        protected virtual void Awake()
        {
            if (carry == null) carry = GetComponent<PlayerCarry>();
        }

        protected virtual void Update()
        {
            if (_unlocked) _charges.Step(UnityEngine.Time.deltaTime);
        }

        /// <summary>Übernimmt die Skillwerte (z. B. nach Investition): max. Ladungen, Cooldown, Freischaltung.</summary>
        public void Configure(int maxCharges, float rechargeSeconds, bool unlocked)
        {
            var wasUnlocked = _unlocked;
            _unlocked = unlocked;
            _charges.Configure(maxCharges, rechargeSeconds);
            if (unlocked && !wasUnlocked) _charges.Refill(); // beim Freischalten sofort einsatzbereit
        }

        /// <summary>Löst die Fähigkeit aus (von der Eingabe / Halten-zum-Wiederholen aufgerufen).</summary>
        public abstract void Activate();

        // Oberstes getragenes Objekt + sein SortKey. False bei leerer Hand.
        protected bool TryGetHeldTop(out Component top, out SortKey key)
        {
            top = null;
            key = default;
            if (carry == null || !carry.TryPeekTopComponent(out top)) return false;
            key = top.TryGetComponent<ISortable>(out var sortable) ? sortable.Key : default;
            return true;
        }

        // Gebäude, in dem der Spieler gerade steht (erster ItemArea-Bereich, der ihn enthält).
        protected ItemArea CurrentArea()
        {
            var areas = Object.FindObjectsByType<ItemArea>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var pos = transform.position;
            foreach (var area in areas)
            {
                if (area != null && area.Contains(pos)) return area;
            }

            return null;
        }
    }
}
