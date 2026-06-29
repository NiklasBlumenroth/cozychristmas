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
    /// Belegt die Regal-Fächer der Geschenkehalle in der offenen Szene (Constitution V: nur Szene), analog
    /// <see cref="DekohalleSortAssignmentSetup"/>. Weist den <c>gabinet</c>-Regalen unter
    /// <c>GeschenkehalleInnen</c> der Reihe nach je eine TOY-Sorte aus dem gemeinsamen
    /// <c>GeschenkehalleCatalog</c> zu (nur Prefabs, die NICHT im <c>Truhengeschenke</c>-Ordner liegen) und
    /// vereinheitlicht alle Fächer eines Regals auf diese eine SortKey. Standard: 2 benachbarte Regale je
    /// Toy (bei 2× so vielen Regalen wie Toys). SortKeys kommen direkt aus den Prefab-<see cref="Sortable"/>,
    /// sind also deckungsgleich. Soll-Mengen/Raster der Fächer bleiben unangetastet.
    /// </summary>
    public static class GeschenkehalleSortAssignmentSetup
    {
        private const string CatalogPath = GeschenkItemSetup.CatalogPath;
        private const string HallRoot = "GeschenkehalleInnen";
        private const string ShelfPrefix = "gabinet";

        [MenuItem("CozySanta/Geschenkehalle/Regale belegen (Toys → Regale, Szene)")]
        public static void Assign()
        {
            var root = FindByName(HallRoot);
            if (root == null)
            {
                Debug.LogError($"[Geschenkehalle] Kein '{HallRoot}' in der offenen Szene gefunden.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[Geschenkehalle] Kein GeschenkehalleCatalog ({CatalogPath}). " +
                               "Zuerst 'Geschenke als Sortierobjekte einrichten' ausführen.");
                return;
            }

            var variants = LoadToyFacets(catalog);
            if (variants.Count == 0)
            {
                Debug.LogError("[Geschenkehalle] Keine Toy-Varianten im Katalog (Prefabs außerhalb " +
                               $"'{GeschenkItemSetup.TruhenFolder}').");
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
                Debug.LogError($"[Geschenkehalle] Keine Fächer unter '{HallRoot}' in Regalen ('{ShelfPrefix}…') gefunden.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            var log = new StringBuilder();

            // Proportionale Zuweisung: bei 2× so vielen Regalen wie Toys -> 2 BENACHBARTE Regale je Toy.
            for (var i = 0; i < shelfRoots.Count; i++)
            {
                var toyIndex = Mathf.Min(i * variants.Count / shelfRoots.Count, variants.Count - 1);
                var facets = variants[toyIndex];
                var shelf = shelfRoots[i];
                foreach (var fach in shelves[shelf].OrderBy(f => f.name))
                {
                    ApplyFacets(fach, facets);
                }

                log.AppendLine($"  {shelf.name} ({shelves[shelf].Count} Fächer) -> [{string.Join(", ", facets)}]");
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);

            Debug.Log($"[Geschenkehalle] {shelfRoots.Count} Regal(e) belegt, {variants.Count} Toy-Varianten " +
                      $"(~{(float)shelfRoots.Count / variants.Count:0.#} Regale je Toy). Szene speichern (Strg+S).\n{log}");
        }

        // Liest die TOY-Varianten (Prefabs NICHT im Truhengeschenke-Ordner) als Sortable-Facetten, Katalog-Reihenfolge.
        private static List<string[]> LoadToyFacets(ItemCatalog catalog)
        {
            var truhenFolder = GeschenkItemSetup.TruhenFolder + "/";
            var result = new List<string[]>();
            foreach (var key in catalog.Keys)
            {
                var prefab = catalog.Get(key);
                if (prefab == null) continue;

                var path = AssetDatabase.GetAssetPath(prefab).Replace('\\', '/');
                if (path.StartsWith(truhenFolder)) continue; // Truhen-Geschenke überspringen

                var sortable = prefab.GetComponent<Sortable>();
                if (sortable == null) continue;

                var so = new SerializedObject(sortable);
                var prop = so.FindProperty("facets");
                if (prop == null || prop.arraySize == 0) continue;

                var facets = new string[prop.arraySize];
                for (var i = 0; i < prop.arraySize; i++)
                    facets[i] = prop.GetArrayElementAtIndex(i).stringValue;
                result.Add(facets);
            }

            return result;
        }

        private static void ApplyFacets(SortTargetInteractable fach, string[] facets)
        {
            Undo.RecordObject(fach, "Geschenkehalle-Regale belegen");
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
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }

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

        private static Transform FindByName(string name)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsDescendantOf(Transform t, Transform root)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                if (cur == root) return true;
            }

            return false;
        }

        private static int NumberOf(Transform t)
        {
            var matches = Regex.Matches(t.name, @"\d+");
            return matches.Count > 0 ? int.Parse(matches[matches.Count - 1].Value) : 0;
        }
    }
}
