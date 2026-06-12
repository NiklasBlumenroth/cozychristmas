using System.Collections.Generic;
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

        private LampBattery _battery;

        /// <summary>Akku-Ladestand 0..1 (Andockpunkt für eine spätere HUD-Anzeige, F7).</summary>
        public float BatteryFraction => _battery != null ? _battery.Fraction : 0f;

        [Tooltip("Empfohlen: Wurzel-Transform der Schnee-Region, die dieser Task zählt. Es werden nur " +
                 "die SnowPatches UNTER dieser Wurzel aggregiert. Leer = ganze Szene (sehr großer Nenner!).")]
        [SerializeField] private Transform coverageRoot;
        [Tooltip("Optional: feste Patch-Liste. Hat Vorrang vor coverageRoot. Leer = coverageRoot bzw. ganze Szene.")]
        [SerializeField] private SnowPatch[] coveragePatches = new SnowPatch[0];

        private SnowPatch[] _coveragePatches;

        /// <summary>Flächen-Fortschritt 0..1 aller erfassten Patches (zellgewichtet). Spiegelt den
        /// gesamten freigelegten Schnee, nicht nur eine einzelne Kachel.</summary>
        public float Coverage
        {
            get
            {
                var patches = _coveragePatches;
                if (patches == null || patches.Length == 0)
                    return patch != null ? patch.Coverage : 0f;

                float cleared = 0f, total = 0f;
                foreach (var p in patches)
                {
                    if (p == null) continue;
                    var cells = p.CellCount;
                    cleared += p.Coverage * cells;
                    total   += cells;
                }
                return total > 0f ? cleared / total : 0f;
            }
        }

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
            // Fortschritts-Region bestimmen: feste Liste > coverageRoot-Teilbaum > ganze Szene.
            if (coveragePatches != null && coveragePatches.Length > 0)
                _coveragePatches = coveragePatches;
            else if (coverageRoot != null)
                _coveragePatches = coverageRoot.GetComponentsInChildren<SnowPatch>(includeInactive: true);
            else
                _coveragePatches = FindObjectsByType<SnowPatch>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var melting = keyboard.fKey.isPressed;
            var adding = keyboard.vKey.isPressed;
            var dt = UnityEngine.Time.deltaTime;

            var origin = viewOrigin != null ? viewOrigin : (Camera.main != null ? Camera.main.transform : transform);
            var hasHit = TryAimAtSnow(origin, out var world, out var aimed);

            // Akku läuft immer wenn F gedrückt, unabhängig ob Schnee getroffen wird
            if (melting && _battery.CanMelt)
            {
                _battery.Drain(drainPerSecond * dt);
                if (hasHit)
                    MeltCone(world, meltStrength * dt);
            }
            else if (adding && hasHit)
            {
                aimed.AddSnow(world, meltRadius, addStrength * dt);
            }

            // Passives Nachladen entfernt (F7): Akku lädt nur noch über die Ladestation auf.
        }

        private readonly Collider[] _coneHits = new Collider[16];
        private readonly HashSet<SnowPatch> _coneSeen = new();

        // Schmilzt den ganzen Pinselkegel: alle SnowPatches im Radius um den Zielpunkt, nicht nur den
        // anvisierten. Jeder Patch senkt den überlappenden Teil – so wirkt der Kegel über Patch-Grenzen.
        private void MeltCone(Vector3 world, float strength)
        {
            _coneSeen.Clear();
            var n = Physics.OverlapSphereNonAlloc(world, meltRadius, _coneHits, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < n; i++)
            {
                var sp = _coneHits[i].GetComponentInParent<SnowPatch>();
                if (sp != null && _coneSeen.Add(sp))
                    sp.Melt(world, meltRadius, strength);
            }
        }

        private bool TryAimAtSnow(Transform origin, out Vector3 world, out SnowPatch aimed)
        {
            world = Vector3.zero;
            aimed = null;
            var ray = new Ray(origin.position, origin.forward);

            // Primär: nächstgelegenes Schnee-Volumen (Trigger-Collider eines beliebigen SnowPatch).
            var hits = Physics.RaycastAll(ray, maxRange, ~0, QueryTriggerInteraction.Collide);
            var bestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                var sp = hit.collider.GetComponentInParent<SnowPatch>();
                if (sp == null || hit.distance >= bestDist) continue;
                bestDist = hit.distance;
                aimed = sp;
                world = hit.point;
            }
            if (aimed != null) return true;

            // Fallback: Schnitt mit der Ebene des primären Patches (falls gesetzt).
            if (patch != null)
            {
                var planePoint = patch.transform.position + (patch.transform.up * (patch.AimHeight * 0.5f));
                var plane = new Plane(patch.transform.up, planePoint);
                if (plane.Raycast(ray, out var enter) && enter <= maxRange)
                {
                    world = ray.GetPoint(enter);
                    aimed = patch;
                    return true;
                }
            }

            return false;
        }
    }
}
