using System.Collections.Generic;
using System.Linq;
using System.Text;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CozySanta.Editor
{
    /// <summary>
    /// Lagerhallen-Pendant zu <see cref="BookshelfSignSetup"/>: setzt auf jedem Lager-Schild
    /// (<c>BoardLagerRegal</c>) das passende Anzeige-Objekt (Box = <c>Crate_01_*</c> oder Kiste =
    /// <c>crateDark_*</c>) entsprechend dem Soll des Bündels. Da in einem Bündel alle Fächer gleich sind,
    /// reicht ein Fach: dessen <c>acceptedFacets</c> = <c>[Typ, Farbe, Form]</c> bestimmt das Item.
    /// Das Schild trägt zwei Platzhalter (Box + Kiste); beide werden ausgeblendet und das benötigte Item
    /// als reine Anzeige am Platz des typgleichen Platzhalters eingesetzt (ohne Rigidbody/Collider/Skripte).
    /// Platzhalter werden nur deaktiviert, nicht gelöscht (Prefab-Verbindung bleibt). Idempotent + Undo-fähig.
    ///
    /// Reiner Editor-Schritt; keine neue Core-Fachlogik → dokumentierte Nicht-Unit-Ausnahme analog
    /// <c>BookshelfSignSetup</c>/<c>BookshelfAssignmentSetup</c>.
    /// </summary>
    public static class LagerSignSetup
    {
        private static readonly string[] ItemFolders =
        {
            "Assets/_Project/Prefabs/Lagerhalle/Box",
            "Assets/_Project/Prefabs/Lagerhalle/Kiste"
        };
        // Schilder heißen in der Szene "BoardRegal" – derselbe Name wie die Bücher-Schilder!
        // Deshalb wird ausschließlich UNTERHALB des Lagerhalle-Objekts gesucht, damit die
        // Bibliotheks-Schilder (unter "BibliothekInnen") garantiert unangetastet bleiben.
        private const string RootName = "Lagerhalle";
        private const string SignName = "BoardRegal";
        private const string DisplaySuffix = " (Anzeige)";

        [MenuItem("CozySanta/Lager/Schild-Objekte aus Fächern setzen")]
        public static void Run()
        {
            var itemByKey = LoadItems();
            if (itemByKey.Count == 0)
            {
                Debug.LogError($"[LagerSchild] Keine Item-Prefabs mit Sortable-Facetten unter {string.Join(", ", ItemFolders)} gefunden.");
                return;
            }

            var roots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == RootName)
                .ToList();
            if (roots.Count == 0)
            {
                Debug.LogWarning($"[LagerSchild] Kein '{RootName}'-Objekt in der offenen Szene gefunden – nichts geändert.");
                return;
            }

            // Unity hängt beim Duplizieren Suffixe an ("BoardRegal (65)"), daher Präfix-Match.
            var signs = roots
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == SignName || t.name.StartsWith(SignName + " ("))
                .Distinct()
                .ToList();
            if (signs.Count == 0)
            {
                Debug.LogWarning($"[LagerSchild] Keine '{SignName}'-Schilder unter '{RootName}' gefunden.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Lager-Schilder setzen");
            var group = Undo.GetCurrentGroup();

            var log = new StringBuilder();
            int done = 0;
            Scene? scene = null;
            foreach (var sign in signs)
            {
                if (ProcessSign(sign, itemByKey, log))
                {
                    done++;
                    scene = sign.gameObject.scene;
                }
            }

            Undo.CollapseUndoOperations(group);
            if (scene.HasValue)
            {
                EditorSceneManager.MarkSceneDirty(scene.Value);
            }

            Debug.Log($"[LagerSchild] {done}/{signs.Count} Schilder bestückt. Szene speichern (Strg+S).\n{log}");
        }

        private static bool ProcessSign(Transform sign, IReadOnlyDictionary<string, GameObject> itemByKey, StringBuilder log)
        {
            CleanupPreviousRun(sign);

            var key = FindBundleKey(sign);
            if (string.IsNullOrEmpty(key))
            {
                log.AppendLine($"  ! {Path(sign)}: kein Bündel-Fach mit acceptedFacets gefunden – übersprungen.");
                return false;
            }

            if (!itemByKey.TryGetValue(key, out var item))
            {
                log.AppendLine($"  ! {Path(sign)}: kein Item-Prefab für [{key.Replace('|', ',')}] – übersprungen.");
                return false;
            }

            var requiredTyp = key.Split('|')[0];
            var placeholders = GetPlaceholders(sign);
            if (placeholders.Count == 0)
            {
                log.AppendLine($"  ! {Path(sign)}: keine Platzhalter (Sortable) am Schild – übersprungen.");
                return false;
            }

            // Anker = Platzhalter gleichen Typs (Box-Item an Box-Platz, Kiste an Kiste-Platz); Fallback: erster.
            var anchor = placeholders.FirstOrDefault(p => TypOf(p) == requiredTyp) ?? placeholders[0];

            PlaceDisplayItem(item, anchor, sign);

            foreach (var ph in placeholders)
            {
                Undo.RecordObject(ph.gameObject, "Platzhalter ausblenden");
                ph.gameObject.SetActive(false);
            }

            log.AppendLine($"  {Path(sign)}: [{key.Replace('|', ',')}] -> {item.name}");
            return true;
        }

        /// <summary>Sucht den nächsten Vorfahren mit Fächern (= das Bündel) und liefert dessen erstes
        /// belegtes <c>acceptedFacets</c> als Schlüssel. Stoppt am ersten fächer-tragenden Vorfahren, damit
        /// nicht versehentlich Fächer anderer Bündel (Lagerhallen-Wurzel) gelesen werden.</summary>
        private static string FindBundleKey(Transform sign)
        {
            for (var t = sign.parent; t != null; t = t.parent)
            {
                var faecher = t.GetComponentsInChildren<SortTargetInteractable>(true)
                    .Where(f => !f.transform.IsChildOf(sign))
                    .ToList();
                if (faecher.Count == 0)
                {
                    continue;
                }

                foreach (var f in faecher)
                {
                    var key = FacetsKey(new SerializedObject(f).FindProperty("acceptedFacets"));
                    if (!string.IsNullOrEmpty(key))
                    {
                        return key;
                    }
                }

                return string.Empty; // Bündel gefunden, aber Fächer noch unbelegt
            }

            return string.Empty;
        }

        private static void PlaceDisplayItem(GameObject item, Transform anchor, Transform sign)
        {
            var go = (GameObject)Object.Instantiate(item);
            go.name = item.name + DisplaySuffix;
            StripToVisual(go);

            var parent = anchor.parent != null ? anchor.parent : sign;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = anchor.localPosition;
            go.transform.localRotation = anchor.localRotation;
            go.transform.localScale = anchor.localScale;

            Undo.RegisterCreatedObjectUndo(go, "Lager-Schild-Objekt setzen");
        }

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

            foreach (var ph in GetPlaceholders(sign))
            {
                if (!ph.gameObject.activeSelf)
                {
                    Undo.RecordObject(ph.gameObject, "Platzhalter reaktivieren");
                    ph.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>Platzhalter-Wurzeln = direkte Kinder des Schilds, deren Teilbaum ein <see cref="Sortable"/>
        /// enthält. Robust dagegen, dass Sortable am Item-Root ODER an einem Kind hängt – so wird beim
        /// Ausblenden/Versetzen stets das ganze Platzhalter-Objekt erfasst, nicht nur ein Unterknoten.</summary>
        private static List<Transform> GetPlaceholders(Transform sign)
        {
            var roots = new HashSet<Transform>();
            foreach (var s in sign.GetComponentsInChildren<Sortable>(true))
            {
                var t = s.transform;
                while (t.parent != null && t.parent != sign)
                {
                    t = t.parent;
                }

                if (t.parent == sign)
                {
                    roots.Add(t);
                }
            }

            return roots.ToList();
        }

        private static void StripToVisual(GameObject root)
        {
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

        private static Dictionary<string, GameObject> LoadItems()
        {
            var map = new Dictionary<string, GameObject>();
            var folders = ItemFolders.Where(AssetDatabase.IsValidFolder).ToArray();
            if (folders.Length == 0)
            {
                return map;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", folders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var sortable = go != null ? go.GetComponentInChildren<Sortable>(true) : null;
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
                    Debug.LogWarning($"[LagerSchild] Doppelter Item-Schlüssel [{key.Replace('|', ',')}] " +
                                     $"({System.IO.Path.GetFileName(path)}) – erstes Prefab wird verwendet.");
                }
            }

            return map;
        }

        private static string TypOf(Transform placeholder)
        {
            var s = placeholder.GetComponentInChildren<Sortable>(true);
            if (s == null)
            {
                return string.Empty;
            }

            var key = FacetsKey(new SerializedObject(s).FindProperty("facets"));
            return string.IsNullOrEmpty(key) ? string.Empty : key.Split('|')[0];
        }

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
