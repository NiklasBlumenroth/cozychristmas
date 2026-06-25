using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Erstellt ein Misch-Girlanden-Material (rote UND grüne Lichter in EINEM Material) auf Basis des
    /// <c>CozySanta/GarlandTwoColor</c>-Shaders und eine Prefab-Variante, die es nutzt. Die Emission-Maske
    /// wird entlang einer Objektraum-Achse abwechselnd eingefärbt – Achse/Frequenz am Material justierbar,
    /// bis der Wechsel pro Birnchen sitzt. Reiner Editor-/Asset-Schritt.
    /// </summary>
    public static class GarlandMixSetup
    {
        private const string Base = "Assets/Christmas pack/";
        private const string BaseTexPath = Base + "Materials/Textures/GarlandLight_a.tga";
        private const string EmisTexPath = Base + "Materials/Textures/GarlandLight_e.tga";
        private const string BasePrefabPath = Base + "Prefabs/Garland_LightRed.prefab";
        private const string MatPath = Base + "Materials/GarlandLightMix.mat";
        private const string OutPrefabPath = Base + "Prefabs/Garland_LightMix.prefab";

        [MenuItem("CozySanta/Deko/Misch-Girlande (Rot+Grün) Material + Prefab erstellen")]
        public static void Create()
        {
            var shader = Shader.Find("CozySanta/GarlandTwoColor");
            if (shader == null)
            {
                Debug.LogError("[Girlande] Shader 'CozySanta/GarlandTwoColor' nicht gefunden – erst kompilieren lassen.");
                return;
            }

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture>(BaseTexPath);
            var emisTex = AssetDatabase.LoadAssetAtPath<Texture>(EmisTexPath);
            if (baseTex == null || emisTex == null)
            {
                Debug.LogError($"[Girlande] Texturen nicht gefunden ({BaseTexPath}, {EmisTexPath}).");
                return;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetTexture("_BaseMap", baseTex);
            mat.SetTexture("_EmissionMap", emisTex);
            mat.SetColor("_BaseTint", Color.white);
            mat.SetColor("_ColorA", new Color(3.0f, 0.10f, 0.10f, 1f));   // Rot (HDR)
            mat.SetColor("_ColorB", new Color(0.10f, 2.6f, 0.20f, 1f));   // Grün (HDR)
            mat.SetVector("_Axis", new Vector4(0f, 1f, 0f, 0f));
            mat.SetFloat("_Frequency", 8f);
            mat.SetFloat("_Phase", 0f);
            mat.SetFloat("_Softness", 0.08f);
            EditorUtility.SetDirty(mat);

            BuildVariant(mat);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Girlande] Misch-Material '{MatPath}' + Variante '{OutPrefabPath}' erstellt. " +
                      "Am Material 'Wechsel-Achse' + 'Wechsel-Frequenz' justieren, bis Rot/Grün pro Birnchen sitzt. " +
                      "Für Glühen: Bloom (URP Volume).");
        }

        private static void BuildVariant(Material mat)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (basePrefab == null)
            {
                Debug.LogWarning($"[Girlande] Basis-Prefab '{BasePrefabPath}' fehlt – nur Material erstellt.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                var renderer = instance.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var mats = renderer.sharedMaterials;
                    for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                    renderer.sharedMaterials = mats;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, OutPrefabPath); // -> Variante von Garland_LightRed
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
