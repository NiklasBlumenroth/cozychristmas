using System.IO;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Editor
{
    /// <summary>
    /// Erzeugt aus <c>crateDark</c> Prefab-Varianten mit einem Symbol auf allen 6 Seiten (mittig).
    /// Je Variante hängt ein Kind „Symbols" mit 6 Quads (eines pro Würfelseite, aus den Bounds der
    /// Basis berechnet), die ein gemeinsames URP/Unlit-Transparent-Material nutzen: <c>symbols_white.png</c>
    /// mit Tiling/Offset auf die jeweilige Atlas-Zelle (4×3) und <c>_BaseColor</c> als Tönung
    /// (Weiß bzw. Schwarz). 12 Symbole × {Weiß, Schwarz} = 24 Varianten. Editor-only.
    /// </summary>
    public static class CrateSymbolVariantSetup
    {
        private const string Atlas     = "Assets/_Project/Textures/symbols_white.png";
        // Geteilte Symbol-Materialien (nur Symbol+Farbe-abhängig, modellunabhängig wiederverwendbar).
        private const string MatFolder = "Assets/_Project/Textures/SymbolMaterials";
        private const int   Cols = 4, Rows = 3;
        private const float SizeFactor = 0.6f; // Symbol-Kantenlänge relativ zur kleineren Flächenseite

        // Atlas-Reihenfolge (zeilenweise, 4×3); Zelle 8 (unten links) = Glocke.
        private static readonly string[] Names =
        {
            "Lebkuchen", "Zuckerstange", "Stern", "Tannenbaum",
            "Flocke", "Geschenk", "Kugel", "Herz",
            "Glocke", "Kerze", "Schneemann", "Schluessel"
        };

        [MenuItem("CozySanta/Modelle/Symbol-Varianten vom ausgewählten Modell (6 Seiten)")]
        public static void Build6() => BuildForSelection(includeTopBottom: true);

        [MenuItem("CozySanta/Modelle/Symbol-Varianten vom ausgewählten Modell (4 Seiten, ohne oben/unten)")]
        public static void Build4() => BuildForSelection(includeTopBottom: false);

        private static void BuildForSelection(bool includeTopBottom)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            if (atlas == null) { Debug.LogError($"[SymbolVariants] Atlas fehlt: {Atlas}"); return; }
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) { Debug.LogError("[SymbolVariants] Shader 'Universal Render Pipeline/Unlit' nicht gefunden."); return; }
            EnsureFolder(MatFolder);

            var models = 0;
            var made = 0;
            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (basePrefab == null) continue; // kein Prefab/Modell
                models++;
                made += BuildVariantsFor(basePrefab, path, includeTopBottom, atlas, shader);
            }

            if (models == 0)
            {
                Debug.LogWarning("[SymbolVariants] Kein Prefab/Modell im Project-Fenster ausgewählt.");
                return;
            }

            AssetDatabase.SaveAssets();
            var sides = includeTopBottom ? "6 Seiten" : "4 Seiten (oben/unten frei)";
            Debug.Log($"[SymbolVariants] {made} Varianten aus {models} Modell(en) erzeugt (Symbol auf {sides}, mittig).");
        }

        private static int BuildVariantsFor(GameObject basePrefab, string basePath, bool includeTopBottom,
            Texture2D atlas, Shader shader)
        {
            var dir = Path.GetDirectoryName(basePath).Replace('\\', '/');
            var baseName = Path.GetFileNameWithoutExtension(basePath);
            var outFolder = $"{dir}/{baseName}_Sym{(includeTopBottom ? "6" : "4")}";
            EnsureFolder(outFolder);

            // Bounds einmalig aus dem Modell bestimmen.
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            probe.transform.position = Vector3.zero;
            probe.transform.rotation = Quaternion.identity;
            var bounds = ComputeBounds(probe);
            Object.DestroyImmediate(probe);
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning($"[SymbolVariants] '{baseName}' hat keine Renderer-Bounds – übersprungen.");
                return 0;
            }

            var tints = new[] { ("Weiss", Color.white), ("Schwarz", Color.black) };
            var made = 0;
            for (var i = 0; i < Names.Length; i++)
            {
                var col = i % Cols;
                var row = i / Cols;
                foreach (var (tintName, tint) in tints)
                {
                    var mat = GetOrCreateMaterial(shader, atlas, col, row, tint, $"Sym_{Names[i]}_{tintName}");

                    var variant = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                    variant.transform.position = Vector3.zero;
                    variant.transform.rotation = Quaternion.identity;
                    AddSymbolQuads(variant, bounds, mat, includeTopBottom);
                    AddCrateComponents(variant, bounds, Names[i]);

                    PrefabUtility.SaveAsPrefabAsset(variant, $"{outFolder}/{baseName}_{Names[i]}_{tintName}.prefab");
                    Object.DestroyImmediate(variant);
                    made++;
                }
            }
            return made;
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void AddSymbolQuads(GameObject root, Bounds b, Material mat, bool includeTopBottom)
        {
            var holder = new GameObject("Symbols");
            holder.transform.SetParent(root.transform, worldPositionStays: false);

            var c = b.center;
            var e = b.extents;
            var eps = (Mathf.Max(e.x, e.y, e.z) * 0.01f) + 0.001f;

            // Seitenflächen (±X, ±Z); oben/unten (±Y) nur bei includeTopBottom.
            var faces = new System.Collections.Generic.List<(Vector3 n, float d1, float d2)>
            {
                (Vector3.right,   e.y, e.z),
                (Vector3.left,    e.y, e.z),
                (Vector3.forward, e.x, e.y),
                (Vector3.back,    e.x, e.y),
            };
            if (includeTopBottom)
            {
                faces.Add((Vector3.up,   e.x, e.z));
                faces.Add((Vector3.down, e.x, e.z));
            }

            foreach (var f in faces)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Symbol";
                var col = q.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);

                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                q.transform.SetParent(holder.transform, worldPositionStays: false);
                var distAlong = Vector3.Scale(e, Abs(f.n)).magnitude; // halbe Ausdehnung entlang der Normale
                q.transform.localPosition = c + (f.n * (distAlong + eps));
                var up = Mathf.Abs(f.n.y) > 0.5f ? Vector3.forward : Vector3.up;
                q.transform.localRotation = Quaternion.LookRotation(f.n, up);
                var side = Mathf.Min(f.d1, f.d2) * 2f * SizeFactor; // d* sind halbe Maße
                q.transform.localScale = new Vector3(side, side, 1f);
            }
        }

        /// <summary>Macht die Kiste aufnehm-/sortierbar – analog zu den Buch-Prefabs: BoxCollider (an die
        /// Bounds gefittet), Rigidbody, PickupInteractable und Sortable (Facette = Symbolname).</summary>
        private static void AddCrateComponents(GameObject root, Bounds b, string symbol)
        {
            // Hinweis: KEIN ?? auf Unity-Objekten – das umgeht Unitys überladenen ==-Operator und kann
            // ein „fake-null"-Component liefern. Daher explizite (== null)-Prüfung + AddComponent.
            var box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            var s = root.transform.lossyScale;
            box.center = root.transform.InverseTransformPoint(b.center);
            box.size = new Vector3(SafeDiv(b.size.x, s.x), SafeDiv(b.size.y, s.y), SafeDiv(b.size.z, s.z));

            var body = root.GetComponent<Rigidbody>();
            if (body == null) body = root.AddComponent<Rigidbody>();
            body.mass = 5f;
            body.useGravity = true;
            body.isKinematic = false;

            var pickup = root.GetComponent<PickupInteractable>();
            if (pickup == null) pickup = root.AddComponent<PickupInteractable>();
            SetFloat(pickup, "weight", 5f);

            var sortable = root.GetComponent<Sortable>();
            if (sortable == null) sortable = root.AddComponent<Sortable>();
            SetStringArray(sortable, "facets", new[] { symbol });
        }

        private static float SafeDiv(float v, float d) => Mathf.Approximately(d, 0f) ? v : v / d;

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetStringArray(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetOrCreateMaterial(Shader shader, Texture2D atlas, int col, int row, Color tint, string name)
        {
            var path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var isNew = mat == null;
            if (isNew) mat = new Material(shader) { name = name };

            mat.shader = shader;
            mat.SetTexture("_BaseMap", atlas);
            mat.SetTextureScale("_BaseMap", new Vector2(1f / Cols, 1f / Rows));
            // UV-Ursprung ist unten links → Atlas-Zeile 0 (oben) auf größten Offset abbilden.
            mat.SetTextureOffset("_BaseMap", new Vector2((float)col / Cols, (float)(Rows - 1 - row) / Rows));
            mat.SetColor("_BaseColor", tint);

            // Transparent + doppelseitig (Symbol auf beiden Quad-Seiten sichtbar).
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;

            if (isNew) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
