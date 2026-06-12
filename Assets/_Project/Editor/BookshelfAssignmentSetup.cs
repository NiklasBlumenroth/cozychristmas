using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Verteilt die Buch-Typen (Facetten <c>[Farbe, Form]</c>) aus <c>Prefabs/Books</c> zufällig und
    /// 1:1 auf die Bookshelf-Fächer (<see cref="SortTargetInteractable"/>) der offenen Szene: je Fach
    /// genau ein Buchtyp, <c>requiredCount = 1</c>, <c>gridSize = (1,1,1)</c>. Die akzeptierten Facetten
    /// werden direkt aus den Buch-Prefabs gelesen (garantiert positionsgleich zu deren SortKey).
    /// Reiner Editor-Schritt; ändert nur die offene Szene (Constitution V konform).
    /// </summary>
    public static class BookshelfAssignmentSetup
    {
        private const string BooksFolder = "Assets/_Project/Prefabs/Books";
        private const string ShelfRootPrefix = "Bookshelf";

        [MenuItem("CozySanta/Bücher/Fächer mit Büchern belegen (zufällig 1:1)")]
        public static void Assign()
        {
            var combos = LoadBookFacets();
            if (combos.Count == 0)
            {
                Debug.LogError($"[FachBelegung] Keine Buch-Prefabs mit Sortable-Facetten unter {BooksFolder} gefunden.");
                return;
            }

            var faecher = CollectShelfFaecher();
            if (faecher.Count == 0)
            {
                Debug.LogError($"[FachBelegung] Keine Bookshelf-Fächer (SortTargetInteractable unter '{ShelfRootPrefix}…') in der offenen Szene gefunden.");
                return;
            }

            if (faecher.Count != combos.Count)
            {
                Debug.LogWarning($"[FachBelegung] Anzahl unterschiedlich: {faecher.Count} Fächer vs. {combos.Count} Buchtypen. " +
                                 $"Es werden {Mathf.Min(faecher.Count, combos.Count)} 1:1 zugeordnet, der Rest bleibt unverändert/ungenutzt.");
            }

            Shuffle(combos);

            var count = Mathf.Min(faecher.Count, combos.Count);
            var log = new StringBuilder();
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();

            for (var i = 0; i < count; i++)
            {
                var fach = faecher[i];
                var facets = combos[i];
                Undo.RecordObject(fach, "Fächer belegen");

                var so = new SerializedObject(fach);
                SetStringArray(so, "acceptedFacets", facets);
                SetInt(so, "requiredCount", 1);
                SetVector3Int(so, "gridSize", 1, 1, 1);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(fach);

                log.AppendLine($"  {HierarchyPath(fach.transform)}  ->  [{string.Join(", ", facets)}]");
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(faecher[0].gameObject.scene);

            Debug.Log($"[FachBelegung] {count} Fächer belegt (je 1 Buchtyp, requiredCount=1, gridSize=1x1x1). " +
                      $"Szene speichern (Strg+S).\n{log}");
        }

        /// <summary>Liest aus jedem Buch-Prefab im Books-Ordner die Sortable-Facetten ([Farbe, Form]).</summary>
        private static List<string[]> LoadBookFacets()
        {
            var result = new List<string[]>();
            if (!AssetDatabase.IsValidFolder(BooksFolder))
            {
                return result;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { BooksFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var sortable = go != null ? go.GetComponent<Sortable>() : null;
                if (sortable == null)
                {
                    continue;
                }

                var so = new SerializedObject(sortable);
                var prop = so.FindProperty("facets");
                if (prop == null || prop.arraySize == 0)
                {
                    Debug.LogWarning($"[FachBelegung] {Path.GetFileName(path)} hat keine Sortable-Facetten; übersprungen.");
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

        /// <summary>Sammelt alle Fächer (SortTargetInteractable) unter einem 'Bookshelf…'-Wurzelobjekt.</summary>
        private static List<SortTargetInteractable> CollectShelfFaecher()
        {
            return Object.FindObjectsByType<SortTargetInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(IsUnderShelf)
                .ToList();
        }

        private static bool IsUnderShelf(SortTargetInteractable fach)
        {
            for (var t = fach.transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith(ShelfRootPrefix))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            // Fisher-Yates; UnityEngine.Random wird im Editor pro Aufruf neu geseedet -> echte Zufallsreihenfolge.
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static string HierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            for (var cur = t; cur != null; cur = cur.parent)
            {
                stack.Push(cur.name);
            }

            return string.Join("/", stack);
        }

        private static void SetStringArray(SerializedObject so, string prop, string[] values)
        {
            var p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogWarning($"[FachBelegung] Feld '{prop}' nicht gefunden.");
                return;
            }

            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private static void SetInt(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p != null)
            {
                p.intValue = value;
            }
        }

        private static void SetVector3Int(SerializedObject so, string prop, int x, int y, int z)
        {
            var p = so.FindProperty(prop);
            if (p == null)
            {
                return;
            }

            p.FindPropertyRelative("x").intValue = x;
            p.FindPropertyRelative("y").intValue = y;
            p.FindPropertyRelative("z").intValue = z;
        }
    }
}
