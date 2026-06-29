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
    /// Durchmischt in der offenen Szene (Constitution V: nur Szene) die Zuweisung der Geschenkehalle:
    /// Jedes Regal (<c>gabinet</c>) bzw. jede Truhe (<see cref="GiftChest"/>) unter <c>GeschenkehalleInnen</c>
    /// bildet ein Bündel aus (a) der einsortierbaren Sorte (<c>acceptedFacets</c> der Fächer bzw. der Truhe)
    /// und (b) den Schild-Abbildern (<c>ItemSchild</c> auf den <c>BoardRegal</c>-Schildern). Beides gehört
    /// zusammen und wird gemeinsam getauscht – die Schild-Meshes werden inklusive ihrer lokalen Position,
    /// Rotation und Skalierung mitgenommen. Die Verteilung ist eine zyklische Zufallspermutation (Sattolo):
    /// jeder Container bekommt garantiert den Inhalt eines ANDEREN. Zwei getrennte Befehle (Regale / Truhen).
    /// Reiner Editor-/Szenen-Schritt (dokumentierte Nicht-Unit-Ausnahme analog der übrigen Setup-Tools).
    /// </summary>
    public static class GeschenkShuffleSetup
    {
        private const string HallRoot = "GeschenkehalleInnen";
        private const string ShelfPrefix = "gabinet";
        private const string BoardPrefix = "BoardRegal";
        private const string SignName = "ItemSchild";

        [MenuItem("CozySanta/Geschenkehalle/Regale durchmischen (Zuweisung + Schild-Mesh)")]
        public static void ShuffleShelves()
        {
            var root = FindByName(HallRoot);
            if (root == null) { Debug.LogError($"[Durchmischen] Kein '{HallRoot}' in der Szene."); return; }
            Shuffle(CollectShelves(root), root, "Regale");
        }

        [MenuItem("CozySanta/Geschenkehalle/Truhen durchmischen (Zuweisung + Schild-Mesh)")]
        public static void ShuffleChests()
        {
            var root = FindByName(HallRoot);
            if (root == null) { Debug.LogError($"[Durchmischen] Kein '{HallRoot}' in der Szene."); return; }
            Shuffle(CollectChests(root), root, "Truhen");
        }

        private sealed class Container
        {
            public Transform root;
            public List<SortTargetInteractable> faecher; // Regale (sonst null)
            public GiftChest chest;                      // Truhen (sonst null)
            public List<Transform> boards;               // BoardRegal-Schilder in Reihenfolge
        }

        private sealed class SignSnapshot
        {
            public GameObject go;       // ItemSchild (null = Board ohne Abbild)
            public Vector3 localPos;
            public Quaternion localRot;
            public Vector3 localScale;
        }

        private sealed class Payload
        {
            public string[] facets;
            public int required = -1;            // nur Truhen, sonst -1
            public List<SignSnapshot> signs;     // parallel zu Container.boards
        }

        private static void Shuffle(List<Container> containers, Transform root, string label)
        {
            if (containers.Count < 2)
            {
                Debug.LogWarning($"[Durchmischen] Nur {containers.Count} {label} gefunden – nichts zu mischen.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();

            // 1) Bündel erfassen, BEVOR etwas verändert wird (Schild-Referenzen + lokale TRS).
            var payloads = containers.Select(Capture).ToList();

            // 2) Zyklische Zufallspermutation: jeder Container bekommt den Inhalt eines anderen.
            var src = Sattolo(containers.Count);

            // 3) Anwenden: Sorte + Schilder vom Quell-Container auf den Ziel-Container übertragen.
            var consumed = new HashSet<GameObject>();
            var log = new StringBuilder();
            for (var i = 0; i < containers.Count; i++)
            {
                var payload = payloads[src[i]];
                ApplyFacets(containers[i], payload);
                MoveSigns(containers[i], payload, consumed);
                log.AppendLine($"  {containers[i].root.name} <- {containers[src[i]].root.name} " +
                               $"[{string.Join(", ", payload.facets ?? new string[0])}]");
            }

            // 4) Überzählige (nicht wiederverwendete) Schilder entfernen (bei ungleicher Board-Anzahl).
            var removed = 0;
            foreach (var p in payloads)
                foreach (var s in p.signs)
                    if (s.go != null && !consumed.Contains(s.go)) { Undo.DestroyObjectImmediate(s.go); removed++; }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log($"[Durchmischen] {containers.Count} {label} durchmischt" +
                      (removed > 0 ? $", {removed} überzählige Schilder entfernt" : "") +
                      $". Szene speichern (Strg+S).\n{log}");
        }

        private static Payload Capture(Container c)
        {
            var p = new Payload { signs = new List<SignSnapshot>() };
            if (c.chest != null)
            {
                var so = new SerializedObject(c.chest);
                p.facets = GetStringArray(so, "acceptedFacets");
                var req = so.FindProperty("required");
                p.required = req != null ? req.intValue : -1;
            }
            else if (c.faecher != null && c.faecher.Count > 0)
            {
                p.facets = GetStringArray(new SerializedObject(c.faecher[0]), "acceptedFacets");
            }

            // Ein Snapshot je Board (go ggf. null) – so bleibt die Board-Reihenfolge zwischen Quelle und Ziel ausgerichtet.
            foreach (var board in c.boards)
            {
                var sign = board.Find(SignName);
                p.signs.Add(sign == null
                    ? new SignSnapshot()
                    : new SignSnapshot
                    {
                        go = sign.gameObject,
                        localPos = sign.localPosition,
                        localRot = sign.localRotation,
                        localScale = sign.localScale,
                    });
            }

            return p;
        }

        private static void ApplyFacets(Container target, Payload payload)
        {
            if (payload.facets == null) return;

            if (target.chest != null)
            {
                Undo.RecordObject(target.chest, "Truhe durchmischen");
                var required = payload.required >= 0 ? payload.required : ReadRequired(target.chest);
                target.chest.SetAccepted(payload.facets, required);
                EditorUtility.SetDirty(target.chest);
                return;
            }

            if (target.faecher == null) return;
            foreach (var fach in target.faecher)
            {
                Undo.RecordObject(fach, "Regal durchmischen");
                var so = new SerializedObject(fach);
                SetStringArray(so, "acceptedFacets", payload.facets);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(fach);
            }
        }

        // Verteilt die Schilder des Quell-Bündels positionsgleich (Board-Index) auf die Ziel-Boards;
        // Mesh + lokale TRS werden mitgenommen. Reicht die Schildzahl nicht, wird das letzte geklont.
        private static void MoveSigns(Container target, Payload payload, HashSet<GameObject> consumed)
        {
            if (payload.signs.Count == 0) return;

            for (var j = 0; j < target.boards.Count; j++)
            {
                var snap = j < payload.signs.Count ? payload.signs[j] : payload.signs[payload.signs.Count - 1];
                if (snap == null || snap.go == null) continue;

                GameObject go;
                if (consumed.Add(snap.go)) go = snap.go;        // Original umhängen (mitnehmen)
                else go = CloneSign(snap.go);                   // mehr Ziel-Boards als Quell-Schilder

                AttachSign(go, target.boards[j], snap);
            }
        }

        private static void AttachSign(GameObject go, Transform board, SignSnapshot snap)
        {
            Undo.SetTransformParent(go.transform, board, "Schild umhängen");
            go.transform.localPosition = snap.localPos;
            go.transform.localRotation = snap.localRot;
            go.transform.localScale = snap.localScale;
            go.name = SignName;
            EditorUtility.SetDirty(go);
        }

        private static GameObject CloneSign(GameObject source)
        {
            var go = Object.Instantiate(source);
            Undo.RegisterCreatedObjectUndo(go, "Schild klonen");
            return go;
        }

        private static List<Container> CollectShelves(Transform root)
        {
            var faecher = Object
                .FindObjectsByType<SortTargetInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(f => IsDescendantOf(f.transform, root));

            var map = new Dictionary<Transform, List<SortTargetInteractable>>();
            foreach (var f in faecher)
            {
                var shelf = NearestAncestorWithPrefix(f.transform, ShelfPrefix);
                if (shelf == null) continue;
                if (!map.TryGetValue(shelf, out var list)) map[shelf] = list = new List<SortTargetInteractable>();
                list.Add(f);
            }

            return map.Keys.OrderBy(NumberOf).ThenBy(t => t.name)
                .Select(s => new Container { root = s, faecher = map[s], boards = BoardsUnder(s) })
                .ToList();
        }

        private static List<Container> CollectChests(Transform root)
        {
            return Object.FindObjectsByType<GiftChest>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => IsDescendantOf(c.transform, root))
                .OrderBy(c => NumberOf(c.transform)).ThenBy(c => c.name)
                .Select(c => new Container { root = c.transform, chest = c, boards = BoardsUnder(c.transform) })
                .ToList();
        }

        private static List<Transform> BoardsUnder(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith(BoardPrefix, System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(NumberOf).ThenBy(t => t.name)
                .ToList();
        }

        // Sattolo-Algorithmus: gleichverteilte zyklische Permutation → garantiert keine Fixpunkte.
        private static int[] Sattolo(int n)
        {
            var a = new int[n];
            for (var i = 0; i < n; i++) a[i] = i;
            var rng = new System.Random();
            for (var i = n - 1; i > 0; i--)
            {
                var j = rng.Next(i); // 0..i-1 (strikt kleiner i)
                var tmp = a[i]; a[i] = a[j]; a[j] = tmp;
            }

            return a;
        }

        private static int ReadRequired(GiftChest chest)
        {
            var prop = new SerializedObject(chest).FindProperty("required");
            return prop != null ? prop.intValue : 100;
        }

        private static string[] GetStringArray(SerializedObject so, string name)
        {
            var prop = so.FindProperty(name);
            if (prop == null) return new string[0];
            var arr = new string[prop.arraySize];
            for (var i = 0; i < prop.arraySize; i++) arr[i] = prop.GetArrayElementAtIndex(i).stringValue;
            return arr;
        }

        private static void SetStringArray(SerializedObject so, string name, string[] values)
        {
            var prop = so.FindProperty(name);
            prop.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static Transform NearestAncestorWithPrefix(Transform t, string prefix)
        {
            for (var cur = t; cur != null; cur = cur.parent)
                if (cur.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return cur;
            return null;
        }

        private static Transform FindByName(string name)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
        }

        private static bool IsDescendantOf(Transform t, Transform root)
        {
            for (var cur = t; cur != null; cur = cur.parent)
                if (cur == root) return true;
            return false;
        }

        private static int NumberOf(Transform t)
        {
            var matches = Regex.Matches(t.name, @"\d+");
            return matches.Count > 0 ? int.Parse(matches[matches.Count - 1].Value) : 0;
        }
    }
}
