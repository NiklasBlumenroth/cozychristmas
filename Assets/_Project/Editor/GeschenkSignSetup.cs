using System.Collections.Generic;
using System.Linq;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Setzt auf jedes <c>BoardRegal</c>-Schild der Geschenkehalle (Kind von Truhe/Regal unter
    /// <c>GeschenkehalleInnen</c>) ein reines Abbild-Mesh des Items, das dort einsortiert werden soll.
    /// Quelle ist der akzeptierte SortKey des Containers (<see cref="GiftChest"/> bzw.
    /// <see cref="SortTargetInteractable"/>) → Prefab aus dem <c>GeschenkehalleCatalog</c>. Das Abbild ist
    /// KEIN Sortierobjekt: alle Komponenten außer <see cref="MeshFilter"/>/<see cref="MeshRenderer"/> werden
    /// entfernt (kein Collider/Rigidbody/Script). Ausrichtung = wie in der Hand (<see cref="SortPlacementRotation.CarryEuler"/>),
    /// zentriert auf der Schildfläche, auf <see cref="FaceFill"/> der Fläche eingepasst, Tiefe (Schild-Normale)
    /// auf <see cref="DepthPercent"/> abgeflacht. Idempotent: ein vorhandenes Abbild wird ersetzt.
    /// Reiner Editor-/Asset-Schritt (dokumentierte Nicht-Unit-Ausnahme).
    /// </summary>
    public static class GeschenkSignSetup
    {
        private const string HallRoot = "GeschenkehalleInnen";
        private const string CatalogPath = GeschenkItemSetup.CatalogPath;
        private const string BoardPrefix = "BoardRegal";
        private const string PreviewName = "ItemSchild";
        private const float FaceFill = 0.8f;       // Anteil der Schildfläche, den das Abbild füllt
        private const float DepthPercent = 0.01f;  // Resttiefe entlang der Schild-Normalen (1 %)
        private const float FrontOffset = 0.002f;  // kleiner Versatz vor die Schildfläche (m)

        [MenuItem("CozySanta/Geschenkehalle/Item-Abbild auf Schilder setzen")]
        public static void Setup()
        {
            var root = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name.IndexOf(HallRoot, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (root == null) { Debug.LogError($"[Schild] Kein '{HallRoot}' in der Szene."); return; }

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null) { Debug.LogError($"[Schild] Kein GeschenkehalleCatalog ({CatalogPath})."); return; }

            var boards = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith(BoardPrefix, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (boards.Count == 0) { Debug.LogError($"[Schild] Keine '{BoardPrefix}'-Schilder unter '{HallRoot}'."); return; }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            int done = 0, skipped = 0;

            foreach (var board in boards)
            {
                var key = ResolveAcceptedKey(board);
                if (string.IsNullOrEmpty(key)) { skipped++; continue; }

                var prefab = catalog.Get(key);
                if (prefab == null) { Debug.LogWarning($"[Schild] '{board.name}': kein Prefab für '{key}'."); skipped++; continue; }

                if (BuildSign(board, prefab)) done++; else skipped++;
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log($"[Schild] {done} Abbild(er) gesetzt, {skipped} übersprungen. Szene speichern (Strg+S).");
        }

        // Liest den akzeptierten SortKey des Containers, zu dem das Schild gehört (Truhe oder Regal).
        private static string ResolveAcceptedKey(Transform board)
        {
            var chest = board.GetComponentInParent<GiftChest>();
            if (chest != null) return FirstFacet(chest, "acceptedFacets");

            var sort = board.GetComponentInParent<SortTargetInteractable>();
            if (sort == null && board.parent != null)
                sort = board.parent.GetComponentInChildren<SortTargetInteractable>(true);
            return sort != null ? FirstFacet(sort, "acceptedFacets") : null;
        }

        private static string FirstFacet(Object comp, string prop)
        {
            var p = new SerializedObject(comp).FindProperty(prop);
            return p != null && p.arraySize > 0 ? p.GetArrayElementAtIndex(0).stringValue : null;
        }

        private static bool BuildSign(Transform board, GameObject prefab)
        {
            // Vorhandenes Abbild entfernen (idempotent).
            var old = board.Find(PreviewName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (inst == null) inst = Object.Instantiate(prefab);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = PreviewName;
            StripToMesh(inst);

            var prefabScale = prefab.transform.localScale;
            if (!TryLocalMeshBounds(inst.transform, out var meshCenter, out var meshSize))
            {
                Debug.LogWarning($"[Schild] '{prefab.name}': kein Mesh.");
                Object.DestroyImmediate(inst);
                return false;
            }

            // Schild-Frame: dünnste lokale Achse = Normale; die beiden anderen = Fläche.
            if (!TryLocalMeshBounds(board, out var bCenter, out var bSize))
            {
                Debug.LogWarning($"[Schild] '{board.name}': kein Schild-Mesh.");
                Object.DestroyImmediate(inst);
                return false;
            }
            int nAxis = MinAxis(bSize);
            int uAxis = (nAxis + 1) % 3, vAxis = (nAxis + 2) % 3;
            var localN = AxisVec(nAxis); var localU = AxisVec(uAxis); var localV = AxisVec(vAxis);

            // Normale nach außen (vom Container-Zentrum weg) ausrichten.
            var containerCenter = board.GetComponentInParent<MeshRenderer>() != null
                ? board.GetComponentInParent<MeshRenderer>().bounds.center
                : board.position;
            var nWorld = board.TransformDirection(localN).normalized;
            if (Vector3.Dot(board.TransformPoint(bCenter) - containerCenter, nWorld) < 0f) { nWorld = -nWorld; localN = -localN; }
            var upWorld = board.TransformDirection(localV).normalized;

            var carry = prefab.TryGetComponent<SortPlacementRotation>(out var spr) ? spr.CarryEuler : Vector3.zero;

            // Parenten und orientieren: lokale Z des Abbilds zeigt entlang der Schild-Normalen (in-Hand-Twist via carry).
            inst.transform.SetParent(board, worldPositionStays: false);
            inst.transform.rotation = Quaternion.LookRotation(nWorld, upWorld) * Quaternion.Euler(carry);

            // Einpassen: Item-XY (im Schild-Frame) auf FaceFill der Schildfläche skalieren.
            MeasureFaceExtents(meshCenter, meshSize, prefabScale, carry, out var extU, out var extV);
            float faceW = bSize[uAxis] * Mathf.Abs(board.lossyScale[uAxis]);
            float faceH = bSize[vAxis] * Mathf.Abs(board.lossyScale[vAxis]);
            float fit = Mathf.Min(FaceFill * faceW / Mathf.Max(extU, 1e-4f),
                                  FaceFill * faceH / Mathf.Max(extV, 1e-4f));

            // Welt-Uniform-Skala 'fit' (über Board-Lossy ausgeglichen), Tiefe (lokale Z) auf 1 %.
            var bl = board.lossyScale;
            inst.transform.localScale = new Vector3(
                fit / NZ(bl.x), fit / NZ(bl.y), (fit * DepthPercent) / NZ(bl.z));

            // Zentrieren auf die Schild-Außenfläche.
            var faceCenterWorld = board.TransformPoint(bCenter + localN * (bSize[nAxis] * 0.5f)) + nWorld * FrontOffset;
            if (TryWorldBoundsCenter(inst.transform, out var curCenter))
                inst.transform.position += faceCenterWorld - curCenter;

            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Undo.RegisterCreatedObjectUndo(inst, "Item-Abbild");
            return true;
        }

        // Entfernt alles außer Transform/MeshFilter/MeshRenderer (Abbild = nur Optik). Erst Skripte
        // (lösen RequireComponent-Abhängigkeiten), dann Physik/Restkomponenten.
        private static void StripToMesh(GameObject go)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null) Object.DestroyImmediate(mb);

            foreach (var c in go.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform || c is MeshFilter || c is MeshRenderer) continue;
                Object.DestroyImmediate(c);
            }
        }

        // Misst die Item-Ausdehnung in der Schildfläche (U,V) nach Anwendung des in-Hand-Twists (carry).
        private static void MeasureFaceExtents(Vector3 c, Vector3 s, Vector3 prefabScale, Vector3 carry,
            out float extU, out float extV)
        {
            var q = Quaternion.Euler(carry);
            var e = s * 0.5f;
            float maxU = 0f, maxV = 0f;
            var baseC = q * Vector3.Scale(c, prefabScale);
            for (int i = 0; i < 8; i++)
            {
                var corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                var f = q * Vector3.Scale(corner, prefabScale) - baseC;
                maxU = Mathf.Max(maxU, Mathf.Abs(f.x));
                maxV = Mathf.Max(maxV, Mathf.Abs(f.y));
            }
            extU = maxU * 2f; extV = maxV * 2f;
        }

        // Kombinierte Mesh-Bounds im lokalen Raum von 'root' (robust bei gedrehten/skalierten Kindern).
        private static bool TryLocalMeshBounds(Transform root, out Vector3 center, out Vector3 size)
        {
            center = Vector3.zero; size = Vector3.zero;
            var bounds = new Bounds(); var has = false;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null) continue;
                var toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var c = mesh.bounds.center; var e = mesh.bounds.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                    var p = toRoot.MultiplyPoint3x4(corner);
                    if (!has) { bounds = new Bounds(p, Vector3.zero); has = true; } else bounds.Encapsulate(p);
                }
            }
            if (!has) return false;
            center = bounds.center; size = bounds.size; return true;
        }

        private static bool TryWorldBoundsCenter(Transform root, out Vector3 center)
        {
            center = Vector3.zero; var bounds = new Bounds(); var has = false;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!has) { bounds = r.bounds; has = true; } else bounds.Encapsulate(r.bounds);
            }
            if (has) center = bounds.center;
            return has;
        }

        private static int MinAxis(Vector3 v)
        {
            int a = 0; float m = Mathf.Abs(v.x);
            if (Mathf.Abs(v.y) < m) { a = 1; m = Mathf.Abs(v.y); }
            if (Mathf.Abs(v.z) < m) a = 2;
            return a;
        }

        private static Vector3 AxisVec(int a) => a == 0 ? Vector3.right : a == 1 ? Vector3.up : Vector3.forward;
        private static float NZ(float v) => Mathf.Approximately(v, 0f) ? 1f : v;
    }
}
