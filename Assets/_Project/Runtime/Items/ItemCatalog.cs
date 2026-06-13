using System.Collections.Generic;
using UnityEngine;

namespace CozySanta.Runtime.Items
{
    /// <summary>
    /// Nachschlagewerk Schlüssel → Prefab für das Wiederherstellen gespeicherter Items. Wird vom
    /// Setup-Tool aus den Item-Prefabs (z. B. allen Büchern) befüllt und vom <see cref="ItemPersistence"/>
    /// beim Laden genutzt.
    /// </summary>
    [CreateAssetMenu(menuName = "CozySanta/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string key;
            public GameObject prefab;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, GameObject> _map;

        /// <summary>Liefert das Prefab zum Schlüssel oder <c>null</c>.</summary>
        public GameObject Get(string key)
        {
            if (_map == null) Build();
            return !string.IsNullOrEmpty(key) && _map.TryGetValue(key, out var prefab) ? prefab : null;
        }

        /// <summary>Ersetzt alle Einträge (für Editor-/Setup-Code).</summary>
        public void SetEntries(List<Entry> list)
        {
            entries = list ?? new List<Entry>();
            _map = null;
        }

        private void Build()
        {
            _map = new Dictionary<string, GameObject>();
            foreach (var e in entries)
            {
                if (e.prefab != null && !string.IsNullOrEmpty(e.key))
                {
                    _map[e.key] = e.prefab;
                }
            }
        }
    }
}
