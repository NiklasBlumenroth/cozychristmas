using System.IO;
using CozySanta.Runtime.DevTools;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Macht die im Deko-Katalog (<c>DekohalleCatalog.asset</c>) gelisteten Sortierobjekte über das DevTool
    /// drehbar. Tut bewusst NUR das – die Items sind bereits anderweitig als Sortierobjekt ausgestattet,
    /// und der Katalog wird NICHT angefasst:
    ///   1. Hängt jeder Katalog-Item-Prefab die <see cref="SortPlacementRotation"/>-Komponente an
    ///      (Dreh-Offset für Ghost + Einlage; Startwert 0, ein bereits gesetzter Wert bleibt erhalten).
    ///   2. Stellt sicher, dass genau EIN <see cref="SortPlacementRotationDevTool"/> in der offenen Szene
    ///      liegt (Tasten I/K J/L U/O; beim Tunen das PerfBisectTool wegen L/U deaktivieren).
    /// Reiner Editor-/Asset-Schritt (Constitution V konform).
    /// </summary>
    public static class DekohalleSortItemSetup
    {
        private const string CatalogPath = "Assets/_Project/Data/DekohalleCatalog.asset";

        [MenuItem("CozySanta/Dekohalle/Deko-Sortierobjekte drehbar machen (SortPlacementRotation + DevTool)")]
        public static void MakeRotatable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[Dekohalle] Katalog nicht gefunden: {CatalogPath}");
                return;
            }

            var added = 0;
            var present = 0;
            foreach (var key in catalog.Keys)
            {
                var prefab = catalog.Get(key);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Dekohalle] Kein Prefab im Katalog für Schlüssel '{key}'.");
                    continue;
                }

                if (AddRotationComponent(prefab)) added++;
                else present++;
            }

            AssetDatabase.SaveAssets();

            var tool = EnsureDevToolInScene();
            Debug.Log($"[Dekohalle] SortPlacementRotation: {added} Prefab(s) neu, {present} bereits vorhanden. " +
                      $"DevTool in Szene {tool}. Drehen: I/K J/L U/O (Shift = fein, P = loggen); " +
                      "PerfBisectTool wegen L/U ggf. kurz deaktivieren.");
        }

        // Hängt SortPlacementRotation an die Prefab-Wurzel, falls noch nicht vorhanden. True = neu hinzugefügt;
        // ein bereits gesetzter Offset bleibt unberührt (kein Überschreiben).
        private static bool AddRotationComponent(GameObject prefabAsset)
        {
            var path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path)) return false;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<SortPlacementRotation>() != null) return false;

                root.AddComponent<SortPlacementRotation>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[Dekohalle] {Path.GetFileName(path)} -> SortPlacementRotation");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Stellt sicher, dass die offene Szene genau ein DevTool enthält; legt es bei Bedarf an
        // (carry wird zur Laufzeit automatisch gesucht). Gibt eine Status-Notiz fürs Log zurück.
        private static string EnsureDevToolInScene()
        {
            var existing = Object.FindAnyObjectByType<SortPlacementRotationDevTool>();
            if (existing != null) return $"vorhanden ('{existing.gameObject.name}')";

            var go = new GameObject("SortPlacementRotationDevTool");
            go.AddComponent<SortPlacementRotationDevTool>();
            Undo.RegisterCreatedObjectUndo(go, "SortPlacementRotationDevTool hinzufügen");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return "neu angelegt";
        }
    }
}
