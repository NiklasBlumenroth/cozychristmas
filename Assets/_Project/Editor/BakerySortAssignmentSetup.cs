using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Belegt die Sortier-Codes der Bäckerei in der offenen Szene (Constitution V: nur Szene). Liest die
    /// SortKeys <c>[Art, Farbe]</c> direkt aus den Süßigkeiten-Prefabs (garantiert positionsgleich zu deren
    /// <see cref="Sortable"/>) und schreibt sie in die <see cref="SortTargetInteractable"/>-Fächer unter
    /// <c>BäckereiInnen</c>: die ersten 4 Warenregale bekommen die Zuckerstangen, die anderen 4 die Lebkuchen
    /// (8 Farben auf je 16 Fächer → jede Farbe doppelt), die Crates je eine Keks-Variante (1:1). Soll-Mengen
    /// und Raster bleiben unangetastet (sind bereits eingestellt: Regal 7×3=21, Crate 75).
    /// </summary>
    public static class BakerySortAssignmentSetup
    {
        private const string SweetsFolder = "Assets/_Project/Prefabs/Süßigkeiten";
        private const string ShelfPrefix = "WarenRegal";
        private const string CratePrefix = "crate";

        // Variante A: Einlege-Sperre nach Art. Regale akzeptieren Zuckerstangen UND Lebkuchen,
        // Crates nur Kekse. (Beeinflusst nur das Einlegen, nicht korrekt/falsch.)
        private static readonly string[] ShelfArts = { "Zuckerstange", "Lebkuchen" };
        private static readonly string[] CrateArts = { "Keks" };

        [MenuItem("CozySanta/Bäckerei/Fächer & Crates belegen (Szene)")]
        public static void Assign()
        {
            var bakery = FindBakeryRoot();
            if (bakery == null)
            {
                Debug.LogError("[Bäckerei] Kein 'BäckereiInnen' in der offenen Szene gefunden.");
                return;
            }

            var sweets = LoadSweetFacets();
            var zucker = sweets.Where(f => f[0] == "Zuckerstange").ToList();
            var lebkuchen = sweets.Where(f => f[0] == "Lebkuchen").ToList();
            var kekse = sweets.Where(f => f[0] == "Keks").ToList();
            if (zucker.Count == 0 || lebkuchen.Count == 0 || kekse.Count == 0)
            {
                Debug.LogError($"[Bäckerei] Süßigkeiten-SortKeys fehlen (Zucker {zucker.Count}, Lebkuchen " +
                               $"{lebkuchen.Count}, Kekse {kekse.Count}). Zuerst 'Süßigkeiten als Sortierobjekte einrichten' ausführen.");
                return;
            }

            var faecher = Object.FindObjectsByType<SortTargetInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(f => IsDescendantOf(f.transform, bakery))
                .ToList();

            var shelves = GroupByAncestor(faecher, ShelfPrefix);
            var crates = GroupByAncestor(faecher, CratePrefix);

            var shelfRoots = shelves.Keys
                .OrderBy(NumberOf).ThenBy(t => t.name)
                .ToList();
            var crateRoots = crates.Keys
                .OrderBy(NumberOf).ThenBy(t => t.name)
                .ToList();

            if (shelfRoots.Count == 0 && crateRoots.Count == 0)
            {
                Debug.LogError($"[Bäckerei] Keine Fächer unter '{bakery.name}' gefunden " +
                               $"(weder '{ShelfPrefix}…' noch '{CratePrefix}…').");
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            var log = new StringBuilder();

            // Regale: erste Hälfte Zuckerstangen, zweite Hälfte Lebkuchen.
            var half = shelfRoots.Count / 2;
            var zuckerShelves = shelfRoots.Take(half).ToList();
            var lebkuchenShelves = shelfRoots.Skip(half).ToList();
            AssignShelves(zuckerShelves, shelves, zucker, "Zuckerstange", log);
            AssignShelves(lebkuchenShelves, shelves, lebkuchen, "Lebkuchen", log);

            // Crates: je eine Keks-Variante (1:1, der Reihe nach).
            var crateIndex = 0;
            foreach (var root in crateRoots)
            {
                foreach (var fach in crates[root].OrderBy(f => f.name))
                {
                    ApplyFacets(fach, kekse[crateIndex % kekse.Count], CrateArts);
                    log.AppendLine($"  {root.name} -> [{string.Join(", ", kekse[crateIndex % kekse.Count])}]");
                    crateIndex++;
                }
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(bakery.gameObject.scene);

            if (shelfRoots.Count != 8)
            {
                Debug.LogWarning($"[Bäckerei] {shelfRoots.Count} Warenregale mit Fächern gefunden (erwartet 8). " +
                                 "Aufteilung erfolgte hälftig – Zuordnung im Log prüfen.");
            }

            Debug.Log($"[Bäckerei] Belegt: {zuckerShelves.Count} Zucker- + {lebkuchenShelves.Count} Lebkuchen-Regale, " +
                      $"{crateRoots.Count} Crate(s). Szene speichern (Strg+S).\n{log}");
        }

        // Verteilt die Farb-SortKeys eines Typs reihum auf alle Fächer der zugehörigen Regale: bei 8 Farben
        // und 16 Fächern landet so jede Farbe in genau 2 Fächern.
        private static void AssignShelves(List<Transform> roots, Dictionary<Transform, List<SortTargetInteractable>> map,
            List<string[]> colors, string label, StringBuilder log)
        {
            var i = 0;
            foreach (var root in roots)
            {
                foreach (var fach in map[root].OrderBy(f => f.name))
                {
                    var facets = colors[i % colors.Count];
                    ApplyFacets(fach, facets, ShelfArts);
                    log.AppendLine($"  {root.name}/{fach.name} -> [{string.Join(", ", facets)}]");
                    i++;
                }
            }

            log.AppendLine($"  ({label}: {i} Fächer auf {roots.Count} Regale)");
        }

        private static void ApplyFacets(SortTargetInteractable fach, string[] facets, string[] placeableArts)
        {
            Undo.RecordObject(fach, "Bäckerei-Fächer belegen");
            var so = new SerializedObject(fach);
            SetStringArray(so, "acceptedFacets", facets);
            // Grobe Einlege-Sperre nach Art (Variante A): Crates nur Kekse, Regale nur Zuckerstange/Lebkuchen.
            SetStringArray(so, "placeableArts", placeableArts);
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

        /// <summary>Liest aus jedem Prefab im Süßigkeiten-Ordner die <see cref="Sortable"/>-Facetten.</summary>
        private static List<string[]> LoadSweetFacets()
        {
            var result = new List<string[]>();
            if (!AssetDatabase.IsValidFolder(SweetsFolder)) return result;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { SweetsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var sortable = go != null ? go.GetComponent<Sortable>() : null;
                if (sortable == null) continue;

                var so = new SerializedObject(sortable);
                var prop = so.FindProperty("facets");
                if (prop == null || prop.arraySize == 0) continue;

                var facets = new string[prop.arraySize];
                for (var i = 0; i < prop.arraySize; i++)
                {
                    facets[i] = prop.GetArrayElementAtIndex(i).stringValue;
                }

                result.Add(facets);
            }

            // Stabile Reihenfolge (Name), damit Wiederholläufe deterministisch belegen.
            return result.OrderBy(f => string.Join("/", f)).ToList();
        }

        // Gruppiert Fächer nach dem nächsten Vorfahren, dessen Name mit dem Präfix beginnt.
        private static Dictionary<Transform, List<SortTargetInteractable>> GroupByAncestor(
            IEnumerable<SortTargetInteractable> faecher, string prefix)
        {
            var map = new Dictionary<Transform, List<SortTargetInteractable>>();
            foreach (var fach in faecher)
            {
                var root = NearestAncestorWithPrefix(fach.transform, prefix);
                if (root == null) continue;
                if (!map.TryGetValue(root, out var list)) map[root] = list = new List<SortTargetInteractable>();
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

        private static Transform FindBakeryRoot()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.IndexOf("ckereiInnen", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsDescendantOf(Transform t, Transform root)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                if (cur == root) return true;
            }

            return false;
        }

        // Letzte Ziffernfolge im Namen (z. B. „WarenRegal (3)" -> 3, „WarenRegal" -> 0).
        private static int NumberOf(Transform t)
        {
            var matches = Regex.Matches(t.name, @"\d+");
            return matches.Count > 0 ? int.Parse(matches[matches.Count - 1].Value) : 0;
        }
    }
}
