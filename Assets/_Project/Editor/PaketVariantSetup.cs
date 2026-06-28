using System.IO;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozySanta.Editor
{
    /// <summary>
    /// Erzeugt aus <c>Paket.prefab</c> 24 Postpaket-Varianten: 12 Symbole × {weißes Symbol, schwarzes
    /// Symbol}. Jede Variante bekommt ein Kind „Symbols" mit 4 Quads (auf den Seitenflächen ±X/±Z, ohne
    /// oben/unten), die die bereits vorhandenen geteilten Symbol-Materialien
    /// (<c>Textures/SymbolMaterials/Sym_{Symbol}_{Weiss|Schwarz}.mat</c>) nutzen – derselbe Symbol-Atlas
    /// wie bei Kisten und Briefen. Die leichte Paket-Physik (Rigidbody/PickupInteractable) bleibt unangetastet;
    /// nur die <see cref="Sortable"/>-Facette wird auf das Symbol gesetzt (analog zu den Brief-Varianten).
    /// Reine Editor/Asset-Manipulation (Constitution V konform). Setzt voraus, dass die Symbol-Materialien
    /// existieren (sonst zuerst „CozySanta/Modelle/Symbol-Varianten …" für ein beliebiges Modell ausführen).
    /// </summary>
    public static class PaketVariantSetup
    {
        private const string BasePrefab  = "Assets/_Project/Prefabs/Paket.prefab";
        private const string MatFolder   = "Assets/_Project/Textures/SymbolMaterials";
        private const string CardboardMat= "Assets/_Project/Materials/M_PaketKarton.mat";
        private const string PrefabFolder= "Assets/_Project/Prefabs/Post/PaketVarianten";

        private const float SizeFactor = 0.6f; // Symbol-Kantenlänge relativ zur kleineren Flächenseite

        // Reihenfolge identisch zu Brief/Kiste – damit Facetten zusammenpassen.
        private static readonly string[] Symbols =
        {
            "Lebkuchen", "Zuckerstange", "Stern", "Tannenbaum",
            "Flocke", "Geschenk", "Kugel", "Herz",
            "Glocke", "Kerze", "Schneemann", "Schluessel"
        };

        // Materialname-Suffix der geteilten Symbol-Materialien.
        private static readonly string[] Tints = { "Weiss", "Schwarz" };

        [MenuItem("CozySanta/Items/Paket-Varianten erzeugen (12 Symbole × Schwarz/Weiss, 4 Seiten)")]
        public static void Build()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
            if (basePrefab == null)
            {
                Debug.LogError($"[PaketVariants] Basis-Prefab fehlt: {BasePrefab}");
                return;
            }
            EnsureFolder(PrefabFolder);

            // Karton-Material (statt der falschen roten Grundfarbe). Fehlt es, erst PaketCardboardSetup.
            var cardboard = AssetDatabase.LoadAssetAtPath<Material>(CardboardMat);
            if (cardboard == null)
            {
                Debug.LogWarning($"[PaketVariants] Karton-Material fehlt: {CardboardMat} – erst " +
                                 "'CozySanta/Items/Paket-Kartonmaterial bauen …' ausführen. " +
                                 "Varianten behalten sonst das Material des Basis-Prefabs.");
            }

            var made = 0;
            var missing = 0;
            foreach (var sym in Symbols)
            {
                foreach (var tint in Tints)
                {
                    var matPath = $"{MatFolder}/Sym_{sym}_{tint}.mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null)
                    {
                        Debug.LogWarning($"[PaketVariants] Symbol-Material fehlt: {matPath}");
                        missing++;
                        continue;
                    }

                    CreateVariant(basePrefab, mat, cardboard, sym, $"Paket_{sym}_{tint}");
                    made++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PaketVariants] {made} Paket-Prefabs erzeugt in {PrefabFolder} " +
                      (missing > 0 ? $"({missing} Symbol-Materialien fehlten)." : "."));
        }

        private static void CreateVariant(GameObject basePrefab, Material symbolMat, Material cardboard,
            string symbol, string name)
        {
            var variant = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            variant.transform.position = Vector3.zero;
            variant.transform.rotation = Quaternion.identity;

            // Karton-Material auf den Paket-Box-Renderer (vor dem Anhängen der Symbol-Quads → trifft nur die Box).
            if (cardboard != null)
            {
                foreach (var mr in variant.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
                {
                    mr.sharedMaterial = cardboard;
                }
            }

            var bounds = ComputeBounds(variant);
            if (bounds.size != Vector3.zero)
            {
                AddSideSymbolQuads(variant, bounds, symbolMat);
            }
            else
            {
                Debug.LogWarning($"[PaketVariants] '{name}' hat keine Renderer-Bounds – Symbol übersprungen.");
            }

            // Sortier-Facette auf das Symbol setzen (Physik/Gewicht des Pakets bleiben wie im Basis-Prefab).
            var sortable = variant.GetComponent<Sortable>();
            if (sortable == null) sortable = variant.AddComponent<Sortable>();
            SetStringArray(sortable, "facets", new[] { symbol });

            PrefabUtility.SaveAsPrefabAsset(variant, $"{PrefabFolder}/{name}.prefab");
            Object.DestroyImmediate(variant);
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // 4 Seitenflächen (±X, ±Z) – oben/unten (±Y) bleiben frei. Mittig auf der Fläche, leicht abgehoben.
        private static void AddSideSymbolQuads(GameObject root, Bounds b, Material mat)
        {
            var holder = new GameObject("Symbols");
            holder.transform.SetParent(root.transform, worldPositionStays: false);

            var c = b.center;
            var e = b.extents;
            var eps = (Mathf.Max(e.x, e.y, e.z) * 0.01f) + 0.001f;

            var faces = new (Vector3 n, float d1, float d2)[]
            {
                (Vector3.right,   e.y, e.z),
                (Vector3.left,    e.y, e.z),
                (Vector3.forward, e.x, e.y),
                (Vector3.back,    e.x, e.y),
            };

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
                q.transform.localRotation = Quaternion.LookRotation(f.n, Vector3.up);
                var side = Mathf.Min(f.d1, f.d2) * 2f * SizeFactor; // d* sind halbe Maße
                q.transform.localScale = new Vector3(side, side, 1f);
            }
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static void SetStringArray(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

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
