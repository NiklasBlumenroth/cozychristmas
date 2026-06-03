using CozySanta.Runtime.Interaction;
using CozySanta.Runtime.Snow;
using UnityEngine;

namespace CozySanta.Runtime.Progression
{
    /// <summary>
    /// Ladestation für die Schmelzlampe. Implementiert <see cref="IInteractable"/> für den
    /// Interaktionshinweis. <see cref="ChargeTick"/> wird von <see cref="CozySanta.Runtime.Player.PlayerInputRelay"/>
    /// aufgerufen solange Rechtsklick + Blickkontakt gehalten werden.
    /// </summary>
    public sealed class LadeStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private float         chargeDuration = 10f;
        [SerializeField] private MeltController melt;

        public string PromptText => melt != null && melt.BatteryFraction >= 1f
            ? "Lampe bereits voll"
            : "Rechtsklick halten: Lampe aufladen";

        /// <summary>Aktueller Akku-Ladestand 0..1 (für HUD-Ladebalken).</summary>
        public float ChargeFraction => melt != null ? melt.BatteryFraction : 0f;

        /// <summary>
        /// Wird pro Frame von PlayerInputRelay aufgerufen solange Rechtsklick + LoS aktiv.
        /// Lädt den Akku proportional zu <paramref name="dt"/> und der konfigurierten Ladedauer.
        /// </summary>
        public void ChargeTick(float dt)
        {
            if (melt == null || chargeDuration <= 0f || dt <= 0f) return;
            var chargePerSecond = melt.BatteryCapacity / chargeDuration;
            melt.ChargeFromStation(chargePerSecond * dt);
        }

        public void Interact() { }
    }
}
