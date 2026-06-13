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

        /// <summary>Anzahl der Katalog-Einträge (Diagnose).</summary>
        public int EntryCount => entries != null ? entries.Count : 0;

        /// <summary>Alle bekannten Schlüssel (z. B. die 96 Buch-Varianten) – für Zählung/Spawn-Quote.
        /// Bewusst ohne Caching: bei deaktiviertem Domain-Reload würde ein einmal gecachter (leerer)
        /// Stand sonst über Play-Sessions hinweg „kleben".</summary>
        public IReadOnlyList<string> Keys
        {
            get
            {
                var keys = new List<string>(entries != null ? entries.Count : 0);
                if (entries != null)
                {
                    foreach (var e in entries)
                    {
                        if (!string.IsNullOrEmpty(e.key) && !keys.Contains(e.key)) keys.Add(e.key);
                    }
                }

                return keys;
            }
        }

        /// <summary>Liefert das Prefab zum Schlüssel oder <c>null</c> (lineare Suche, ohne Cache).</summary>
        public GameObject Get(string key)
        {
            if (string.IsNullOrEmpty(key) || entries == null) return null;
            foreach (var e in entries)
            {
                if (e.key == key) return e.prefab;
            }

            return null;
        }

        /// <summary>Ersetzt alle Einträge (für Editor-/Setup-Code).</summary>
        public void SetEntries(List<Entry> list)
        {
            entries = list ?? new List<Entry>();
        }
    }
}
