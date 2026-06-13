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

        // Settle-Dauer (s), die das Setup-Tool an allen Buch-Prefabs setzt. Großzügig, da nur Authoring.
        private const float SettleDuration = 3f;

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

                // SettlingBody sicherstellen UND die Settle-Dauer explizit setzen, damit ein erneutes
                // Ausführen den Wert auch an bereits ausgestatteten Prefabs aktualisiert.
                var settling = root.GetComponent<SettlingBody>() ?? root.AddComponent<SettlingBody>();
                var sso = new SerializedObject(settling);
                sso.FindProperty("settleDuration").floatValue = SettleDuration;
                sso.ApplyModifiedPropertiesWithoutUndo();

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

            var player = Object.FindAnyObjectByType<CozySanta.Runtime.Player.FirstPersonController>();
            var playerTf = player != null ? player.transform : null;
            var cameraTf = Camera.main != null ? Camera.main.transform : playerTf;

            var host = GameObject.Find("ItemPersistence") ?? new GameObject("ItemPersistence");
            var persistence = host.GetComponent<ItemPersistence>() ?? host.AddComponent<ItemPersistence>();
            if (host.GetComponent<CozySanta.Runtime.DevTools.ItemSaveDevTool>() == null)
            {
                host.AddComponent<CozySanta.Runtime.DevTools.ItemSaveDevTool>();
            }

            var spawner = host.GetComponent<AreaSpawner>() ?? host.AddComponent<AreaSpawner>();
            var hud = host.GetComponent<CozySanta.Runtime.DevTools.AreaInventoryHud>()
                      ?? host.AddComponent<CozySanta.Runtime.DevTools.AreaInventoryHud>();

            SetRef(persistence, "catalog", catalog);
            SetRef(spawner, "persistence", persistence);
            SetRef(spawner, "player", playerTf);
            SetRef(spawner, "spawnOrigin", cameraTf);
            SetRef(hud, "persistence", persistence);
            SetRef(hud, "player", playerTf);

            var areasAdded = AttachAreasToZones(catalog);

            // Fallback: existiert kein Bereich mit Katalog (z. B. keine AreaZone in der Szene),
            // eine Start-ItemArea „Bibliothek" als BoxCollider-Volumen anlegen.
            var createdFallback = EnsureLibraryArea(catalog);

            // DevSpawnMenu reagiert ebenfalls auf „R" -> deaktivieren, sonst doppeltes Spawnen.
            var disabled = 0;
            foreach (var dev in Object.FindObjectsByType<CozySanta.Runtime.DevTools.DevSpawnMenu>(FindObjectsSortMode.None))
            {
                if (dev.enabled) { dev.enabled = false; EditorUtility.SetDirty(dev); disabled++; }
            }

            EditorUtility.SetDirty(host);
            var fallbackNote = createdFallback != null
                ? $" Start-Bereich '{createdFallback.name}' angelegt – bitte Box im Scene-View auf den Bibliotheksraum skalieren/positionieren!"
                : "";
            Debug.Log($"[ItemPersistence] Szenen-Objekt '{host.name}' bereit; {areasAdded} Bereich(e) aus AreaZones angelegt; " +
                      $"{disabled} DevSpawnMenu deaktiviert.{fallbackNote} " +
                      "R = Zufallsbuch spawnen (halten = mehrere), F6 = Inventar/Buttons, F4 = Speicher-Menü (Play-Mode).");
        }

        // Legt eine ItemArea „Bibliothek" an, falls noch kein Bereich einen Katalog hat. Volumen aus den
        // Renderer-Bounds eines „Bibliothek"-Objekts (sonst Standardgröße). Rückgabe: erstelltes Objekt oder null.
        private static GameObject EnsureLibraryArea(ItemCatalog booksCatalog)
        {
            foreach (var existing in Object.FindObjectsByType<ItemArea>(FindObjectsSortMode.None))
            {
                if (existing.Catalog != null) return null; // schon ein Bereich mit Items konfiguriert
            }

            var center = Vector3.zero;
            var size = new Vector3(12f, 6f, 12f);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name.ToLowerInvariant().Contains("biblio"))
                {
                    center = t.position;
                    var renderers = t.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var b = renderers[0].bounds;
                        for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                        center = b.center;
                        size = b.size;
                    }

                    break;
                }
            }

            var go = new GameObject("Bibliothek_ItemArea");
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = size;

            var area = go.AddComponent<ItemArea>();
            var so = new SerializedObject(area);
            so.FindProperty("areaName").stringValue = "Bibliothek";
            so.FindProperty("catalog").objectReferenceValue = booksCatalog;
            so.FindProperty("maxPerVariant").intValue = 20;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        // Hängt an jede AreaZone eine ItemArea (Name aus AreaTracker). Bibliotheks-Bereiche bekommen
        // zusätzlich den Bücher-Katalog (Max 20) – andere Gebäude konfiguriert man später im Inspector.
        private static int AttachAreasToZones(ItemCatalog booksCatalog)
        {
            var added = 0;
            foreach (var zone in Object.FindObjectsByType<CozySanta.Runtime.Areas.AreaZone>(FindObjectsSortMode.None))
            {
                var area = zone.GetComponent<ItemArea>() ?? zone.gameObject.AddComponent<ItemArea>();
                var name = zone.Tracker != null ? zone.Tracker.AreaName : zone.gameObject.name;

                var so = new SerializedObject(area);
                so.FindProperty("areaName").stringValue = name;
                if (name != null && name.ToLowerInvariant().Contains("biblio"))
                {
                    so.FindProperty("catalog").objectReferenceValue = booksCatalog;
                    so.FindProperty("maxPerVariant").intValue = 20;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                added++;
            }

            return added;
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
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
