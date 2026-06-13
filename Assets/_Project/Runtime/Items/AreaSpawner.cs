using CozySanta.Core.Input;
using CozySanta.Core.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.Items
{
    /// <summary>
    /// Spawnt per Taste „R" im aktuell betretenen Bereich ein zufälliges, noch nicht gedeckeltes Item
    /// (z. B. eines der 96 Bücher der Bibliothek) mit zufälliger XYZ-Rotation vor dem Spieler. Welche
    /// Variante erlaubt ist, entscheidet die testbare <see cref="SpawnQuota"/> aus den aktuellen
    /// Stückzahlen (<see cref="ItemPersistence.CountByKey"/>) und der Höchstzahl des Bereichs. Sind alle
    /// Varianten voll, passiert nichts. Generisch über den Bereichs-Katalog – für andere Gebäude
    /// (z. B. Kisten in der Lagerhalle) genügt ein anderer Katalog an der jeweiligen <see cref="ItemArea"/>.
    /// </summary>
    public sealed class AreaSpawner : MonoBehaviour
    {
        [SerializeField] private ItemPersistence persistence;
        [Tooltip("Spieler-Transform zur Bestimmung des aktuellen Bereichs (Fallback: spawnOrigin/dieses).")]
        [SerializeField] private Transform player;
        [Tooltip("Ursprung für den Spawn (z. B. die Kamera). Fallback: dieses Transform.")]
        [SerializeField] private Transform spawnOrigin;
        [Tooltip("Optionaler Eltern-Transform für gespawnte Items.")]
        [SerializeField] private Transform spawnParent;
        [SerializeField] private float distance = 1.2f;
        [SerializeField] private float heightOffset = 0.3f;
        [Tooltip("Zufällige Streuung der Spawn-Position (m), damit Bücher beim Halten nicht exakt überlappen.")]
        [SerializeField] private float scatterRadius = 0.3f;

        [Header("R gedrückt halten = wiederholt spawnen")]
        [Tooltip("Wartezeit nach dem ersten Spawn, bevor die Auto-Wiederholung startet (s).")]
        [SerializeField] private float holdInitialDelay = 0.35f;
        [Tooltip("Abstand zwischen den Spawns beim Halten von R (s).")]
        [SerializeField] private float holdRepeatInterval = 0.08f;

        [Tooltip("Diagnose-Ausgaben in der Console (zum Debuggen).")]
        [SerializeField] private bool debugLog = true;

        private HoldRepeatTimer _spawnRepeat;

        private void Awake()
        {
            if (persistence == null) persistence = FindAnyObjectByType<ItemPersistence>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var pressed = keyboard != null && keyboard.rKey.isPressed;

            // Diagnose: jeder echte R-Druck schreibt einen vollständigen Statusbericht.
            if (debugLog && keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                Debug.Log("[AreaSpawner] R erkannt. " +
                          (persistence == null ? "persistence=NULL!" : DescribeNoArea()));
            }

            if (_spawnRepeat.Tick(pressed, UnityEngine.Time.deltaTime, holdInitialDelay, holdRepeatInterval))
            {
                var ok = TrySpawn();
                if (debugLog && ok) Debug.Log("[AreaSpawner] Buch gespawnt.");
            }
        }

        /// <summary>Spawnt ein erlaubtes Zufalls-Item im aktuellen Bereich. False, wenn nichts möglich war.</summary>
        public bool TrySpawn()
        {
            if (persistence == null)
            {
                LogReason("Keine ItemPersistence verdrahtet.");
                return false;
            }

            var area = CurrentArea();
            if (area == null || area.Catalog == null)
            {
                LogReason(DescribeNoArea());
                return false;
            }

            var counts = persistence.CountByKey(area);
            if (!SpawnQuota.TryPick(area.Catalog.Keys, counts, area.MaxPerVariant, Random.value, out var key))
            {
                LogReason($"'{area.AreaName}': alle {area.Catalog.Keys.Count} Varianten voll " +
                          $"(Katalog '{area.Catalog.name}', Einträge {area.Catalog.EntryCount}, Max {area.MaxPerVariant}).");
                return false;
            }

            var prefab = area.Catalog.Get(key);
            if (prefab == null)
            {
                LogReason($"Kein Prefab im Katalog für Schlüssel '{key}'.");
                return false;
            }

            var origin = spawnOrigin != null ? spawnOrigin : transform;
            var scatter = Random.insideUnitCircle * scatterRadius;
            var position = origin.position + (origin.forward * distance) + (Vector3.up * heightOffset)
                           + new Vector3(scatter.x, 0f, scatter.y);
            Instantiate(prefab, position, Random.rotationUniform, spawnParent);
            return true;
        }

        private ItemArea CurrentArea()
        {
            // Nur Bereiche mit Katalog sind spawnbar (bei überlappenden Zonen den richtigen treffen).
            var probe = ProbePosition();
            foreach (var area in persistence.FindAreas())
            {
                if (area != null && area.Catalog != null && area.Contains(probe)) return area;
            }

            return null;
        }

        private Vector3 ProbePosition()
        {
            if (player != null) return player.position;
            if (spawnOrigin != null) return spawnOrigin.position;
            return transform.position;
        }

        // Baut eine erklärende Meldung, warum am Spielerstandort kein spawnbarer Bereich gefunden wurde.
        private string DescribeNoArea()
        {
            var probe = ProbePosition();
            var areas = persistence.FindAreas();
            var msg = $"Kein spawnbarer Bereich am Standort {probe}. Bereiche ({areas.Length}): ";
            foreach (var a in areas)
            {
                if (a == null) continue;
                msg += $"[{a.AreaName}: Katalog={(a.Catalog != null ? "ja" : "NEIN")}, " +
                       $"Collider={a.ColliderCount}, enthält Spieler={a.Contains(probe)}] ";
            }

            return msg;
        }

        private float _nextLogTime;

        private void LogReason(string message)
        {
            // Gedrosselt, damit das Halten von R die Console nicht flutet.
            if (UnityEngine.Time.unscaledTime < _nextLogTime) return;
            _nextLogTime = UnityEngine.Time.unscaledTime + 1.5f;
            Debug.Log("[AreaSpawner] " + message);
        }
    }
}
