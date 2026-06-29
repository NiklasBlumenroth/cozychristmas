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
    /// Macht die im <c>GeschenkehalleCatalog</c> gelisteten Sortierobjekte über das DevTool fein justierbar
    /// (analog <see cref="DekohalleSortItemSetup"/>). Tut bewusst NUR das – die Items sind bereits als
    /// Sortierobjekt ausgestattet, und der Katalog wird NICHT angefasst:
    ///   1. Hängt jedem Katalog-Item-Prefab die <see cref="SortPlacementRotation"/>-Komponente an
    ///      (Dreh-Offset/Größe/Höhe für Ghost + Einlage; Startwerte neutral, bereits gesetzte bleiben erhalten).
    ///   2. Stellt sicher, dass genau EIN <see cref="SortPlacementRotationDevTool"/> in der offenen Szene liegt.
    /// Hinweis: Die Justage wirkt auf die FACH-Einlage (Regale/Toys); die Truhen-Geschenke fallen frei in die
    /// Truhe und nutzen sie nicht (die Komponente schadet dort aber nicht). Reiner Editor-/Asset-Schritt.
    /// </summary>
    public static class GeschenkSortItemSetup
    {
        private const string CatalogPath = GeschenkItemSetup.CatalogPath;

        [MenuItem("CozySanta/Geschenkehalle/Geschenke drehbar machen (SortPlacementRotation + DevTool)")]
        public static void MakeRotatable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[Geschenkehalle] Katalog nicht gefunden: {CatalogPath}. " +
                               "Zuerst 'Geschenke als Sortierobjekte einrichten' ausführen.");
                return;
            }

            var added = 0;
            var present = 0;
            foreach (var key in catalog.Keys)
            {
                var prefab = catalog.Get(key);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Geschenkehalle] Kein Prefab im Katalog für Schlüssel '{key}'.");
                    continue;
                }

                if (AddRotationComponent(prefab)) added++;
                else present++;
            }

            AssetDatabase.SaveAssets();

            var tool = EnsureDevToolInScene();
            Debug.Log($"[Geschenkehalle] SortPlacementRotation: {added} Prefab(s) neu, {present} bereits vorhanden. " +
                      $"DevTool in Szene {tool}. Item tragen, im Fach-Ghost justieren: I/K J/L U/O = drehen, " +
                      "‚/. = Größe, Bild↑/Bild↓ = Höhe (Shift = fein, P = loggen).");
        }

        // Hängt SortPlacementRotation an die Prefab-Wurzel, falls noch nicht vorhanden. Ein bereits gesetzter
        // Offset bleibt unberührt (kein Überschreiben).
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
                Debug.Log($"[Geschenkehalle] {Path.GetFileName(path)} -> SortPlacementRotation");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Stellt sicher, dass die offene Szene genau ein DevTool enthält; legt es bei Bedarf an.
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
