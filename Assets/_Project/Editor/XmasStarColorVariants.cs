using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Erzeugt Farbvarianten des Prefabs <c>Dekohalle/Xmas Tree Star</c>. Je Farbe entsteht ein Material
    /// (Kopie des Basis-Materials – Shader, Metallic/Smoothness bleiben für den Glanz erhalten, aber die
    /// Palette-Textur wird entfernt → reine, kräftige Vollfarbe in <c>_BaseColor</c>) und eine eigenständige
    /// Prefab-Kopie, die dieses Material nutzt. Idempotent (erneutes Ausführen aktualisiert Material/Prefab).
    /// Reiner Editor-/Asset-Schritt (Constitution V).
    /// </summary>
    public static class XmasStarColorVariants
    {
        private const string BasePrefab = "Assets/_Project/Prefabs/Dekohalle/Xmas Tree Star.prefab";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Dekohalle";
        private const string MatFolder = "Assets/_Project/Prefabs/Dekohalle/XmasStarMaterials";

        // Zielfarben (Name -> RGB) als reine Vollfarbe in _BaseColor (Palette-Textur wird entfernt).
        private static readonly (string name, Color color)[] Colors =
        {
            ("Rot",    new Color(0.80f, 0.08f, 0.08f)),
            ("Rosa",   new Color(1.00f, 0.62f, 0.74f)),
            ("Blau",   new Color(0.10f, 0.30f, 0.90f)),
            ("Grün",   new Color(0.10f, 0.60f, 0.18f)),
            ("Pink",   new Color(0.95f, 0.10f, 0.55f)),
            ("Silber", new Color(0.82f, 0.84f, 0.88f)),
            ("Orange", new Color(1.00f, 0.45f, 0.04f)),
        };

        [MenuItem("CozySanta/Dekohalle/Xmas Tree Star – Farbvarianten erzeugen")]
        public static void Build()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
            if (basePrefab == null)
            {
                Debug.LogError($"[XmasStar] Basis-Prefab fehlt: {BasePrefab}");
                return;
            }

            var baseMat = FindBaseMaterial(basePrefab);
            if (baseMat == null)
            {
                Debug.LogError("[XmasStar] Kein Material am Basis-Prefab gefunden.");
                return;
            }

            EnsureFolder(MatFolder);

            var made = 0;
            foreach (var (name, color) in Colors)
            {
                var mat = GetOrCreateTintedMaterial(baseMat, color, $"M_XmasStar_{name}");

                // Eigenständige Kopie (kein Variant): instanziieren, komplett entpacken, Material setzen, speichern.
                var variant = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                PrefabUtility.UnpackPrefabInstance(variant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                variant.name = $"Xmas Tree Star {name}";

                foreach (var mr in variant.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
                {
                    var mats = mr.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++)
                    {
                        mats[i] = mat;
                    }

                    mr.sharedMaterials = mats;
                }

                PrefabUtility.SaveAsPrefabAsset(variant, $"{PrefabFolder}/Xmas Tree Star {name}.prefab");
                Object.DestroyImmediate(variant);
                made++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[XmasStar] {made} Farbvarianten erzeugt in '{PrefabFolder}' " +
                      $"(Materialien in '{MatFolder}').");
        }

        private static Material FindBaseMaterial(GameObject prefab)
        {
            foreach (var mr in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                if (mr.sharedMaterial != null)
                {
                    return mr.sharedMaterial;
                }
            }

            return null;
        }

        private static Material GetOrCreateTintedMaterial(Material baseMat, Color color, string name)
        {
            var path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var isNew = mat == null;

            if (isNew)
            {
                mat = new Material(baseMat) { name = name };
            }
            else
            {
                // Vorhandenes Material auf den Basis-Stand bringen (Shader/Textur), dann neu tönen.
                mat.CopyPropertiesFromMaterial(baseMat);
            }

            // Palette-Textur entfernen → reine Vollfarbe (sonst tönt _BaseColor nur die Textur und wirkt matschig).
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", null);
            }

            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", null);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }

            return mat;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
