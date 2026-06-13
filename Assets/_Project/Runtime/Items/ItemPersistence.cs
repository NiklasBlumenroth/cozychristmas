using System.Collections.Generic;
using UnityEngine;

namespace CozySanta.Runtime.Items
{
    /// <summary>
    /// Verbindet Bereiche (<see cref="ItemArea"/>), Katalog und Datei-Persistenz: speichert die
    /// aktuellen aufnehmbaren Items eines Bereichs (Pose) und lädt sie zurück. Beim Szenenstart wird
    /// optional jeder Bereich automatisch geladen (ersetzt vorhandene Items im Bereich) – so erscheint
    /// der „chaotische Haufen" sofort und ohne Live-Physik, weil geladene Items direkt ruhen.
    /// </summary>
    public sealed class ItemPersistence : MonoBehaviour
    {
        [SerializeField] private ItemCatalog catalog;
        [Tooltip("Optionaler Eltern-Transform für geladene Items (Ordnung in der Hierarchie).")]
        [SerializeField] private Transform spawnParent;
        [Tooltip("Beim Szenenstart alle Bereiche automatisch laden.")]
        [SerializeField] private bool loadOnStart = true;

        private void Start()
        {
            if (loadOnStart) LoadAll();
        }

        /// <summary>Alle Bereiche der Szene.</summary>
        public ItemArea[] FindAreas() =>
            FindObjectsByType<ItemArea>(FindObjectsSortMode.None);

        /// <summary>Speichert die aufnehmbaren Items innerhalb des Bereichs. Gibt die Anzahl zurück.</summary>
        public int SaveArea(ItemArea area)
        {
            if (area == null) return 0;

            var placements = new List<ItemPlacement>();
            foreach (var id in FindObjectsByType<PrefabId>(FindObjectsSortMode.None))
            {
                if (id == null || string.IsNullOrEmpty(id.Key)) continue;

                var t = id.transform;
                if (!area.Contains(t.position)) continue;

                placements.Add(new ItemPlacement { key = id.Key, position = t.position, rotation = t.rotation });
            }

            return AreaItemStore.Save(area.AreaName, placements);
        }

        /// <summary>Lädt den gespeicherten Zustand eines Bereichs (ersetzt vorhandene Items darin).</summary>
        public int LoadArea(ItemArea area)
        {
            if (area == null || catalog == null || !AreaItemStore.TryLoad(area.AreaName, out var data))
            {
                return 0;
            }

            ClearArea(area);

            var spawned = 0;
            foreach (var p in data.items)
            {
                var prefab = catalog.Get(p.key);
                if (prefab == null)
                {
                    Debug.LogWarning($"[ItemPersistence] Kein Prefab im Katalog für Schlüssel '{p.key}'.");
                    continue;
                }

                var go = Instantiate(prefab, p.position, p.rotation, spawnParent);
                // Geladene Items starten sofort ruhend (kinematisch) -> kein Settle-Spike.
                if (go.TryGetComponent<SettlingBody>(out var settling)) settling.EnterRest();
                spawned++;
            }

            return spawned;
        }

        /// <summary>Lädt alle Bereiche der Szene.</summary>
        public int LoadAll()
        {
            var total = 0;
            foreach (var area in FindAreas())
            {
                total += LoadArea(area);
            }

            return total;
        }

        /// <summary>Zerstört alle aufnehmbaren Items innerhalb des Bereichs.</summary>
        public void ClearArea(ItemArea area)
        {
            if (area == null) return;

            foreach (var id in FindObjectsByType<PrefabId>(FindObjectsSortMode.None))
            {
                if (id != null && area.Contains(id.transform.position))
                {
                    Destroy(id.gameObject);
                }
            }
        }
    }
}
