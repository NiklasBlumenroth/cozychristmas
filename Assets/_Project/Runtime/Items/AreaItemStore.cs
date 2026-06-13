using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CozySanta.Runtime.Items
{
    /// <summary>
    /// Liest/schreibt die Item-Pose eines Bereichs als JSON unter <c>StreamingAssets</c>
    /// (eine Datei je Bereich, z. B. <c>items_Bibliothek.json</c>). StreamingAssets ist im Editor
    /// beschreibbar und im fertigen Build (PC) lesbar – eignet sich damit als ausgelieferter
    /// Startzustand. Schreibt nur Daten, keine Seiteneffekte auf Szenenobjekte.
    /// </summary>
    public static class AreaItemStore
    {
        public static string FilePathFor(string area) =>
            Path.Combine(Application.streamingAssetsPath, $"items_{Sanitize(area)}.json");

        /// <summary>Speichert die übergebenen Platzierungen für <paramref name="area"/>. Gibt die Anzahl zurück.</summary>
        public static int Save(string area, List<ItemPlacement> placements)
        {
            var data = new AreaItemData { area = area, items = placements ?? new List<ItemPlacement>() };
            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.WriteAllText(FilePathFor(area), JsonUtility.ToJson(data, true));
            return data.items.Count;
        }

        /// <summary>Lädt die gespeicherten Platzierungen eines Bereichs. False, wenn keine Datei existiert.</summary>
        public static bool TryLoad(string area, out AreaItemData data)
        {
            data = null;
            var path = FilePathFor(area);
            if (!File.Exists(path)) return false;

            data = JsonUtility.FromJson<AreaItemData>(File.ReadAllText(path));
            return data != null;
        }

        private static string Sanitize(string area)
        {
            if (string.IsNullOrEmpty(area)) return "Bereich";
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                area = area.Replace(c, '_');
            }

            return area.Replace(' ', '_');
        }
    }
}
