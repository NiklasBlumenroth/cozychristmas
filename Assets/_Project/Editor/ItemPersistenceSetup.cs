using System.Collections.Generic;
using System.IO;
using CozySanta.Runtime.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Editor
{
    /// <summary>
    /// Richtet die Item-Persistenz ein (Constitution V: nur Editor-/Asset-Manipulation). Stattet alle
    /// Buch-Prefabs mit <see cref="PrefabId"/> (Schlüssel = Prefab-Name) und <see cref="SettlingBody"/>
    /// aus, schaltet ihren Schattenwurf ab (Perf bei tausenden Büchern) und baut einen
    /// <see cref="ItemCatalog"/>. Ein zweiter Befehl legt die Szenen-Objekte
    /// (<see cref="ItemPersistence"/> + Dev-Menü) an und verknüpft Bereiche mit den vorhandenen Zonen.
    /// </summary>
    public static class ItemPersistenceSetup
    {
        private const string BooksFolder = "Assets/_Project/Prefabs/Books";
        private const string DataFolder = "Assets/_Project/Data";
        private const string CatalogPath = DataFolder + "/ItemCatalog.asset";

        [MenuItem("CozySanta/Items/Buch-Persistenz einrichten (Prefabs + Katalog)")]
        public static void SetupBooks()
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { BooksFolder });
            var entries = new List<ItemCatalog.Entry>();
            var stamped = 0;

            foreach (var guid in paths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = Path.GetFileNameWithoutExtension(path);
                if (StampPrefab(path, key)) stamped++;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) entries.Add(new ItemCatalog.Entry { key = key, prefab = prefab });
            }

            BuildCatalog(entries);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemPersistence] {stamped} Buch-Prefab(s) ausgestattet, Katalog mit {entries.Count} Einträgen unter {CatalogPath}.");
        }

        private static bool StampPrefab(string path, string key)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return false;

            try
            {
                var id = root.GetComponent<PrefabId>() ?? root.AddComponent<PrefabId>();
                id.SetKey(key);

                if (root.GetComponent<SettlingBody>() == null)
                {
                    root.AddComponent<SettlingBody>();
                }

                // Schattenwurf der Bücher aus: im Haufen kaum sichtbar, spart den Schatten-Pass.
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildCatalog(List<ItemCatalog.Entry> entries)
        {
            EnsureFolder(DataFolder);

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
        }

        [MenuItem("CozySanta/Items/Szenen-Objekte anlegen (Persistence + Menü + Bereiche)")]
        public static void SetupScene()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[ItemPersistence] Kein Katalog unter {CatalogPath} – zuerst 'Buch-Persistenz einrichten' ausführen.");
                return;
            }

            var host = GameObject.Find("ItemPersistence") ?? new GameObject("ItemPersistence");
            var persistence = host.GetComponent<ItemPersistence>() ?? host.AddComponent<ItemPersistence>();
            if (host.GetComponent<CozySanta.Runtime.DevTools.ItemSaveDevTool>() == null)
            {
                host.AddComponent<CozySanta.Runtime.DevTools.ItemSaveDevTool>();
            }

            var so = new SerializedObject(persistence);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();

            var areasAdded = AttachAreasToZones();
            EditorUtility.SetDirty(host);
            Debug.Log($"[ItemPersistence] Szenen-Objekt '{host.name}' bereit; {areasAdded} Bereich(e) aus AreaZones angelegt. " +
                      "Dev-Menü: Taste F4 im Play-Mode.");
        }

        // Hängt an jede AreaZone eine ItemArea (Bereichsname aus deren AreaTracker), falls noch keine da ist.
        private static int AttachAreasToZones()
        {
            var added = 0;
            foreach (var zone in Object.FindObjectsByType<CozySanta.Runtime.Areas.AreaZone>(FindObjectsSortMode.None))
            {
                if (zone.GetComponent<ItemArea>() != null) continue;

                var area = zone.gameObject.AddComponent<ItemArea>();
                var name = zone.Tracker != null ? zone.Tracker.AreaName : zone.gameObject.name;
                var so = new SerializedObject(area);
                so.FindProperty("areaName").stringValue = name;
                so.ApplyModifiedPropertiesWithoutUndo();
                added++;
            }

            return added;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
