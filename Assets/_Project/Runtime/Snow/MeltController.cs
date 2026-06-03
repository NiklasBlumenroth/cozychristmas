using CozySanta.Core.Snow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.Snow
{
    /// <summary>
    /// Treibt das Schmelzen (Apply): Raycast aus dem Blick auf die Schnee-Fläche, dann pro Tick das
    /// Core-<see cref="LampBattery"/> + das <see cref="SnowPatch"/> ansteuern. Taste F (halten) = schmelzen
    /// (verbraucht Akku), Taste V = Schnee auftragen (Dev-Helfer). Akku lädt langsam nach, wenn nicht
    /// geschmolzen wird. Zeit-/Eingabezugriffe sind Laufzeit; die Entscheidungen liegen in der Core-Schicht.
    /// </summary>
    public sealed class MeltController : MonoBehaviour
    {
        [SerializeField] private Transform viewOrigin;
        [SerializeField] private SnowPatch patch;

        [Header("Lampe")]
        [SerializeField] private float maxRange = 6f;
        [SerializeField] private float meltRadius = 0.8f;
        [SerializeField] private float meltStrength = 1.2f;   // Höhe/Sekunde im Zentrum
        [SerializeField] private float addStrength = 1.2f;

        [Header("Akku")]
        [SerializeField] private float batteryCapacity = 12f;
        [SerializeField] private float drainPerSecond = 1f;
        [SerializeField] private float rechargePerSecond = 0.6f;

        private LampBattery _battery;

        /// <summary>Akku-Ladestand 0..1 (Andockpunkt für eine spätere HUD-Anzeige, F7).</summary>
        public float BatteryFraction => _battery != null ? _battery.Fraction : 0f;

        /// <summary>Flächen-Fortschritt 0..1 des angesteuerten Patches.</summary>
        public float Coverage => patch != null ? patch.Coverage : 0f;

        /// <summary>Maximale Akku-Kapazität. Andockpunkt für LampBattery-Upgrade (F6).</summary>
        public float BatteryCapacity
        {
            get => batteryCapacity;
            set { batteryCapacity = value; if (_battery != null) _battery.Capacity = value; }
        }

        /// <summary>Schmelzstärke (Höhe/s im Zentrum). Andockpunkt für LampPower-Upgrade (F6).</summary>
        public float MeltStrength { get => meltStrength; set => meltStrength = value; }

        /// <summary>Schmelzradius (m). Andockpunkt für LampCone-Upgrade (F6).</summary>
        public float MeltRadius { get => meltRadius; set => meltRadius = value; }

        /// <summary>Lädt den Akku von einer externen Quelle auf (Ladestation, F7).</summary>
        public void ChargeFromStation(float amount)
        {
            if (_battery != null) _battery.Recharge(amount);
        }

        private void Awake()
        {
            _battery = new LampBattery(batteryCapacity);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || patch == null)
            {
                return;
            }

            var melting = keyboard.fKey.isPressed;
            var adding = keyboard.vKey.isPressed;
            var dt = UnityEngine.Time.deltaTime;

            var origin = viewOrigin != null ? viewOrigin : (Camera.main != null ? Camera.main.transform : transform);
            var hasHit = TryAimAtSnow(origin, out var world);

            var didMelt = false;
            if (melting && _battery.CanMelt && hasHit)
            {
                if (patch.Melt(world, meltRadius, meltStrength * dt))
                {
                    _battery.Drain(drainPerSecond * dt);
                    didMelt = true;
                }
            }
            else if (adding && hasHit)
            {
                patch.AddSnow(world, meltRadius, addStrength * dt);
            }

            // Nachladen immer, wenn in diesem Frame nicht aktiv geschmolzen wird – auch bei gehaltenem F
            // (sonst Deadlock: leerer Akku bei gehaltenem F würde nie wieder laden).
            if (!didMelt)
            {
                _battery.Recharge(rechargePerSecond * dt);
            }
        }

        private bool TryAimAtSnow(Transform origin, out Vector3 world)
        {
            world = Vector3.zero;
            var ray = new Ray(origin.position, origin.forward);

            // Primär: Strahl gegen das tatsächliche Schnee-Volumen (Trigger-Collider am Patch).
            var hits = Physics.RaycastAll(ray, maxRange, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<SnowPatch>() == patch)
                {
                    world = hits[i].point;
                    return true;
                }
            }

            // Fallback: Schnitt mit der Patch-Ebene auf halber Schneehöhe.
            var planePoint = patch.transform.position + (patch.transform.up * (patch.AimHeight * 0.5f));
            var plane = new Plane(patch.transform.up, planePoint);
            if (plane.Raycast(ray, out var enter) && enter <= maxRange)
            {
                world = ray.GetPoint(enter);
                return true;
            }

            return false;
        }
    }
}
