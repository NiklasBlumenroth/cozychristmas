using System.IO;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Sorting;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Erzeugt aus den vorgerenderten Brief-Atlas-Texturen (Assets/_Project/Textures/BriefVariants)
    /// 24 Brief-Prefabs: 12 Symbole × {schwarzer Brief + weißes Symbol, weißer Brief + schwarzes Symbol}.
    /// Jedes Prefab nutzt das gemeinsame <c>Brief_Mesh</c> (Vorderseite Faltung+Symbol, Rückseite Symbol,
    /// Kanten Papier) und ein eigenes URP/Lit-Material. Analog zu den Kisten werden die Briefe aufnehm-
    /// und sortierbar gemacht (BoxCollider, Rigidbody, PickupInteractable, Sortable mit Facette = Symbol).
    /// Reine Editor/Asset-Manipulation. Texturen vorher per Generator erzeugt.
    /// </summary>
    public static class BriefVariantSetup
    {
        private const string MeshPath    = "Assets/_Project/Meshes/Brief_Mesh.asset";
        private const string TexFolder   = "Assets/_Project/Textures/BriefVariants";
        private const string MatFolder   = "Assets/_Project/Materials/BriefVariants";
        private const string PrefabFolder= "Assets/_Project/Prefabs/Post/Varianten";

        private const float BriefMass   = 0.2f; // leichter als Kisten (Brief)
        private static readonly Vector3 BriefScale = new Vector3(0.2f, 0.1f, 0.01f);

        // Reihenfolge identisch zum Kisten-Setup (CrateSymbolVariantSetup) – damit Facetten zusammenpassen.
        private static readonly string[] Symbols =
        {
            "Lebkuchen", "Zuckerstange", "Stern", "Tannenbaum",
            "Flocke", "Geschenk", "Kugel", "Herz",
            "Glocke", "Kerze", "Schneemann", "Schluessel"
        };

        // (Schema-Suffix der Textur, Material/Prefab-Suffix). Schwarz = schwarzer Brief/weißes Symbol.
        private static readonly (string scheme, string label)[] Schemes =
        {
            ("Schwarz", "Schwarz"),
            ("Weiss",   "Weiss"),
        };

        [MenuItem("CozySanta/Items/Brief-Varianten erzeugen (12 Symbole × Schwarz/Weiss)")]
        public static void Build()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh == null)
            {
                Debug.LogError($"[BriefVariants] {MeshPath} fehlt. Erst 'CozySanta/Items/Brief-Mesh bauen' ausführen.");
                return;
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[BriefVariants] Shader 'Universal Render Pipeline/Lit' nicht gefunden (URP aktiv?).");
                return;
            }
            EnsureFolder(MatFolder);
            EnsureFolder(PrefabFolder);

            var made = 0;
            var missing = 0;
            foreach (var sym in Symbols)
            {
                foreach (var (scheme, label) in Schemes)
                {
                    var texPath = $"{TexFolder}/T_Brief_{sym}_{scheme}.png";
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                    if (tex == null)
                    {
                        Debug.LogWarning($"[BriefVariants] Textur fehlt: {texPath}");
                        missing++;
                        continue;
                    }

                    var mat = CreateMaterial(shader, tex, $"M_Brief_{sym}_{label}");
                    CreatePrefab(mesh, mat, sym, $"Brief_{sym}_{label}");
                    made++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BriefVariants] {made} Brief-Prefabs erzeugt in {PrefabFolder} " +
                      (missing > 0 ? $"({missing} Texturen fehlten)." : "."));
        }

        private static Material CreateMaterial(Shader shader, Texture2D tex, string name)
        {
            var path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var isNew = mat == null;
            if (isNew) mat = new Material(shader) { name = name };

            mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.2f);

            if (isNew) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreatePrefab(Mesh mesh, Material mat, string symbol, string name)
        {
            var go = new GameObject(name);
            go.transform.localScale = BriefScale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // Unit-Box-Mesh → BoxCollider 1×1×1 (durch Scale auf Briefmaß).
            var box = go.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = Vector3.one;

            var body = go.AddComponent<Rigidbody>();
            body.mass = BriefMass;
            body.useGravity = true;
            body.isKinematic = false;

            var pickup = go.AddComponent<PickupInteractable>();
            SetFloat(pickup, "weight", BriefMass);

            var sortable = go.AddComponent<Sortable>();
            SetStringArray(sortable, "facets", new[] { symbol });

            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{name}.prefab");
            Object.DestroyImmediate(go);
        }

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
