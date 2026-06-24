using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Belegt die Sortier-Codes der Dekohalle in der offenen Szene (Constitution V: nur Szene). Weist den
    /// Regalen (<c>gabinet</c>-Prefab-Instanzen) unter <c>DekoInnen</c> der Reihe nach je eine Deko-Variante
    /// aus dem <c>DekohalleCatalog</c> zu und vereinheitlicht ALLE Fächer eines Regals auf diese eine SortKey
    /// <c>[Art, Variante]</c>. Reihenfolge der Regale = letzte Ziffer im Namen (<c>gabinet</c>,
    /// <c>gabinet (1)</c>, …), Reihenfolge der Varianten = Katalog. Die SortKeys werden direkt aus den Prefabs
    /// (deren <see cref="Sortable"/>) gelesen, sind also garantiert deckungsgleich. Soll-Mengen und Raster der
    /// Fächer bleiben unangetastet.
    /// </summary>
    public static class DekohalleSortAssignmentSetup
    {
        private const string CatalogPath = "Assets/_Project/Data/DekohalleCatalog.asset";
        private const string ShelfPrefix = "gabinet";

        [MenuItem("CozySanta/Dekohalle/Fächer in Regalen belegen (Szene)")]
        public static void Assign()
        {
            var root = FindDekoRoot();
            if (root == null)
            {
                Debug.LogError("[Dekohalle] Kein 'DekoInnen' in der offenen Szene gefunden.");
                return;
            }

            var variants = LoadCatalogFacets();
            if (variants.Count == 0)
            {
                Debug.LogError($"[Dekohalle] Keine Varianten-SortKeys aus dem Katalog ({CatalogPath}) gelesen. " +
                               "Zuerst die Deko-Prefabs als Sortierobjekte einrichten.");
                return;
            }

            var faecher = Object
                .FindObjectsByType<SortTargetInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(f => IsDescendantOf(f.transform, root))
                .ToList();

            var shelves = GroupByAncestor(faecher, ShelfPrefix);
            var shelfRoots = shelves.Keys.OrderBy(NumberOf).ThenBy(t => t.name).ToList();
            if (shelfRoots.Count == 0)
            {
                Debug.LogError($"[Dekohalle] Keine Fächer unter '{root.name}' in Regalen ('{ShelfPrefix}…') gefunden.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            var log = new StringBuilder();

            // Pro Regal eine Variante (Katalog-Reihenfolge); alle Fächer des Regals auf dieselbe SortKey setzen.
            for (var i = 0; i < shelfRoots.Count; i++)
            {
                var facets = variants[i % variants.Count];
                var shelf = shelfRoots[i];
                foreach (var fach in shelves[shelf].OrderBy(f => f.name))
                {
                    ApplyFacets(fach, facets);
                }

                log.AppendLine($"  {shelf.name} ({shelves[shelf].Count} Fächer) -> [{string.Join(", ", facets)}]");
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);

            if (shelfRoots.Count != variants.Count)
            {
                Debug.LogWarning($"[Dekohalle] {shelfRoots.Count} Regale, aber {variants.Count} Katalog-Varianten. " +
                                 (shelfRoots.Count < variants.Count
                                     ? $"Die letzten {variants.Count - shelfRoots.Count} Variante(n) haben kein Regal."
                                     : "Überzählige Regale wiederholen Varianten (modulo)."));
            }

            Debug.Log($"[Dekohalle] {shelfRoots.Count} Regal(e) belegt (je 1 Variante, alle Fächer gleich). " +
                      $"Szene speichern (Strg+S).\n{log}");
        }

        private static void ApplyFacets(SortTargetInteractable fach, string[] facets)
        {
            Undo.RecordObject(fach, "Dekohalle-Fächer belegen");
            var so = new SerializedObject(fach);
            SetStringArray(so, "acceptedFacets", facets);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(fach);
        }

        private static void SetStringArray(SerializedObject so, string name, string[] values)
        {
            var prop = so.FindProperty(name);
            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        // Liest aus jeder Katalog-Variante (Prefab) die Sortable-Facetten, in Katalog-Reihenfolge.
        private static List<string[]> LoadCatalogFacets()
        {
            var result = new List<string[]>();
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                return result;
            }

            foreach (var key in catalog.Keys)
            {
                var prefab = catalog.Get(key);
                var sortable = prefab != null ? prefab.GetComponent<Sortable>() : null;
                if (sortable == null)
                {
                    Debug.LogWarning($"[Dekohalle] '{key}': kein Sortable am Prefab – übersprungen.");
                    continue;
                }

                var so = new SerializedObject(sortable);
                var prop = so.FindProperty("facets");
                if (prop == null || prop.arraySize == 0)
                {
                    continue;
                }

                var facets = new string[prop.arraySize];
                for (var i = 0; i < prop.arraySize; i++)
                {
                    facets[i] = prop.GetArrayElementAtIndex(i).stringValue;
                }

                result.Add(facets);
            }

            return result;
        }

        // Gruppiert Fächer nach dem nächsten Vorfahren, dessen Name mit dem Präfix beginnt (= das Regal).
        private static Dictionary<Transform, List<SortTargetInteractable>> GroupByAncestor(
            IEnumerable<SortTargetInteractable> faecher, string prefix)
        {
            var map = new Dictionary<Transform, List<SortTargetInteractable>>();
            foreach (var fach in faecher)
            {
                var shelf = NearestAncestorWithPrefix(fach.transform, prefix);
                if (shelf == null) continue;
                if (!map.TryGetValue(shelf, out var list)) map[shelf] = list = new List<SortTargetInteractable>();
                list.Add(fach);
            }

            return map;
        }

        private static Transform NearestAncestorWithPrefix(Transform t, string prefix)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                if (cur.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return cur;
            }

            return null;
        }

        private static Transform FindDekoRoot()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.IndexOf("DekoInnen", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsDescendantOf(Transform t, Transform root)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                if (cur == root) return true;
            }

            return false;
        }

        // Letzte Ziffernfolge im Namen (z. B. „gabinet (3)" -> 3, „gabinet" -> 0) für stabile Regal-Reihenfolge.
        private static int NumberOf(Transform t)
        {
            var matches = Regex.Matches(t.name, @"\d+");
            return matches.Count > 0 ? int.Parse(matches[matches.Count - 1].Value) : 0;
        }
    }
}
