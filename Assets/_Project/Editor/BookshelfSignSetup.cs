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
    /// Setzt auf jedem Bookshelf-Schild (<c>BoardRegal</c>) unter <c>BibliothekInnen</c> die 4 Platzhalter-Bücher
    /// auf die tatsächlich in den 4 Fächern (<see cref="SortTargetInteractable"/>) benötigten Bücher um:
    /// liest je Fach die <c>acceptedFacets</c>, sucht das passende Buch-Prefab (über <see cref="Sortable"/>-
    /// Facetten, 1:1 wie <c>BookshelfAssignmentSetup</c>) und hängt eine reine Anzeige-Kopie ans Schild –
    /// der Platzhalter wird nur deaktiviert (Unity verbietet das Löschen von Prefab-Instanz-Kindern; so bleibt
    /// die Bookshelf-Prefab-Verbindung erhalten). Zuordnung Fach↔Slot über die Namens-Nummerierung
    /// (z. B. "Fach (2)" ↔ "BuchBlauFlocke (2)"), "ohne Suffix" = Index 0.
    ///
    /// Reiner Editor-Schritt (ändert nur die offene Szene, Undo-fähig, idempotent). Keine neue Core-Fachlogik
    /// → dokumentierte Nicht-Unit-Ausnahme, analog <c>BookshelfAssignmentSetup</c>/<c>BookPrefabSetup</c>.
    /// </summary>
    public static class BookshelfSignSetup
    {
        private const string BooksFolder = "Assets/_Project/Prefabs/Books";
        private const string RootName = "BibliothekInnen";
        private const string ShelfPrefix = "Bookshelf";
        private const string SignName = "BoardRegal";
        private const string DisplaySuffix = " (Anzeige)";

        [MenuItem("CozySanta/Bücher/Schild-Bücher aus Fächern setzen")]
        public static void Run()
        {
            var bookByKey = LoadBookPrefabs();
            if (bookByKey.Count == 0)
            {
                Debug.LogError($"[SchildBücher] Keine Buch-Prefabs mit Sortable-Facetten unter {BooksFolder} gefunden.");
                return;
            }

            var root = FindByName(RootName);
            if (root == null)
            {
                Debug.LogError($"[SchildBücher] Kein Objekt '{RootName}' in der offenen Szene gefunden.");
                return;
            }

            var shelves = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith(ShelfPrefix))
                .ToList();
            if (shelves.Count == 0)
            {
                Debug.LogWarning($"[SchildBücher] Keine '{ShelfPrefix}…'-Objekte unter '{RootName}' gefunden.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Schild-Bücher setzen");
            var group = Undo.GetCurrentGroup();

            var log = new StringBuilder();
            int shelvesDone = 0, slotsSet = 0;
            foreach (var shelf in shelves)
            {
                var set = ProcessShelf(shelf, bookByKey, log);
                if (set > 0)
                {
                    shelvesDone++;
                    slotsSet += set;
                }
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log($"[SchildBücher] {shelvesDone} Regale bearbeitet, {slotsSet} Schild-Bücher gesetzt. " +
                      $"Szene speichern (Strg+S).\n{log}");
        }

        /// <summary>Bearbeitet ein Regal; liefert die Anzahl gesetzter Schild-Bücher.</summary>
        private static int ProcessShelf(Transform shelf, IReadOnlyDictionary<string, GameObject> bookByKey, StringBuilder log)
        {
            var sign = shelf.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == SignName);
            if (sign == null)
            {
                return 0; // Regal ohne Schild – stillschweigend überspringen
            }

            CleanupPreviousRun(sign);

            var placeholders = sign.GetComponentsInChildren<Sortable>(true)
                .Select(s => s.transform)
                .OrderBy(t => SuffixIndex(t.name))
                .ThenBy(t => t.GetSiblingIndex())
                .ToList();

            var faecher = shelf.GetComponentsInChildren<SortTargetInteractable>(true)
                .Where(f => !f.transform.IsChildOf(sign))
                .OrderBy(f => SuffixIndex(f.name))
                .ThenBy(f => f.transform.GetSiblingIndex())
                .ToList();

            if (placeholders.Count == 0 || faecher.Count == 0)
            {
                log.AppendLine($"  ! {Path(shelf)}: {placeholders.Count} Platzhalter / {faecher.Count} Fächer – übersprungen.");
                return 0;
            }

            if (placeholders.Count != faecher.Count)
            {
                log.AppendLine($"  ! {Path(shelf)}: {placeholders.Count} Platzhalter ≠ {faecher.Count} Fächer – es werden {Mathf.Min(placeholders.Count, faecher.Count)} gesetzt.");
            }

            var count = Mathf.Min(placeholders.Count, faecher.Count);
            var done = 0;
            for (var i = 0; i < count; i++)
            {
                var key = AcceptedKey(faecher[i]);
                if (string.IsNullOrEmpty(key))
                {
                    log.AppendLine($"  ! {Path(faecher[i].transform)}: kein acceptedFacets gesetzt – Slot {i} bleibt Platzhalter.");
                    continue;
                }

                if (!bookByKey.TryGetValue(key, out var book))
                {
                    log.AppendLine($"  ! {Path(faecher[i].transform)}: kein Buch-Prefab für [{key.Replace('|', ',')}] – Slot {i} bleibt Platzhalter.");
                    continue;
                }

                PlaceDisplayBook(book, placeholders[i], sign);
                done++;
            }

            if (done > 0)
            {
                log.AppendLine($"  {Path(shelf)}: {done} Bücher gesetzt.");
            }

            return done;
        }

        /// <summary>Instanziiert das Buch als reine Optik am Platz des Platzhalters und blendet den Platzhalter aus.</summary>
        private static void PlaceDisplayBook(GameObject book, Transform placeholder, Transform sign)
        {
            // Object.Instantiate erzeugt im Editor einen verbindungslosen Klon (kein Prefab-Bezug)
            // -> Komponenten frei entfernbar, als "added GameObject" unter dem Schild ablegbar.
            var go = (GameObject)Object.Instantiate(book);
            go.name = book.name + DisplaySuffix;
            StripToVisual(go);

            var parent = placeholder.parent != null ? placeholder.parent : sign;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = placeholder.localPosition;
            go.transform.localRotation = placeholder.localRotation;
            go.transform.localScale = placeholder.localScale;

            Undo.RegisterCreatedObjectUndo(go, "Schild-Buch setzen");

            Undo.RecordObject(placeholder.gameObject, "Platzhalter ausblenden");
            placeholder.gameObject.SetActive(false);
        }

        /// <summary>Entfernt Anzeige-Bücher früherer Läufe und reaktiviert ausgeblendete Platzhalter (idempotent).</summary>
        private static void CleanupPreviousRun(Transform sign)
        {
            var stale = sign.GetComponentsInChildren<Transform>(true)
                .Where(t => t != sign && t.name.EndsWith(DisplaySuffix))
                .Select(t => t.gameObject)
                .Distinct()
                .ToList();
            foreach (var g in stale)
            {
                Undo.DestroyObjectImmediate(g);
            }

            foreach (var s in sign.GetComponentsInChildren<Sortable>(true))
            {
                if (!s.gameObject.activeSelf)
                {
                    Undo.RecordObject(s.gameObject, "Platzhalter reaktivieren");
                    s.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Entfernt alles außer reiner Render-Optik (Transform/MeshFilter/MeshRenderer bleiben).</summary>
        private static void StripToVisual(GameObject root)
        {
            // Reihenfolge wegen möglicher RequireComponent-Abhängigkeiten: erst Skripte, dann Physik.
            foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (c != null) Object.DestroyImmediate(c);
            }

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb != null) Object.DestroyImmediate(rb);
            }

            foreach (var col in root.GetComponentsInChildren<Collider>(true))
            {
                if (col != null) Object.DestroyImmediate(col);
            }
        }

        /// <summary>Baut die Map Facetten-Schlüssel → Buch-Prefab aus dem Books-Ordner.</summary>
        private static Dictionary<string, GameObject> LoadBookPrefabs()
        {
            var map = new Dictionary<string, GameObject>();
            if (!AssetDatabase.IsValidFolder(BooksFolder))
            {
                return map;
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

                var key = FacetsKey(new SerializedObject(sortable).FindProperty("facets"));
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (!map.ContainsKey(key))
                {
                    map[key] = go;
                }
                else
                {
                    Debug.LogWarning($"[SchildBücher] Doppelter Buch-Schlüssel [{key.Replace('|', ',')}] " +
                                     $"({System.IO.Path.GetFileName(path)}) – erstes Prefab wird verwendet.");
                }
            }

            return map;
        }

        private static string AcceptedKey(SortTargetInteractable fach)
            => FacetsKey(new SerializedObject(fach).FindProperty("acceptedFacets"));

        private static string FacetsKey(SerializedProperty arrayProp)
        {
            if (arrayProp == null || arrayProp.arraySize == 0)
            {
                return string.Empty;
            }

            var parts = new string[arrayProp.arraySize];
            for (var i = 0; i < arrayProp.arraySize; i++)
            {
                parts[i] = arrayProp.GetArrayElementAtIndex(i).stringValue;
            }

            return string.Join("|", parts);
        }

        /// <summary>Liefert die Zahl in einem abschließenden " (n)" oder 0 ("ohne Suffix" = erster Slot).</summary>
        private static int SuffixIndex(string name)
        {
            var m = Regex.Match(name, @"\((\d+)\)\s*$");
            return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : 0;
        }

        private static Transform FindByName(string name)
            => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);

        private static string Path(Transform t)
        {
            var stack = new Stack<string>();
            for (var cur = t; cur != null; cur = cur.parent)
            {
                stack.Push(cur.name);
            }

            return string.Join("/", stack);
        }
    }
}
