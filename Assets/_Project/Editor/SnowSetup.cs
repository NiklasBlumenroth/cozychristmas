using CozySanta.Runtime.Snow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für F5: erzeugt das Schnee-Material (Shader + stilisierte Snow-Textur), legt einen
    /// Schnee-Patch über dem Boden an und verdrahtet den <see cref="MeltController"/> am Player
    /// (Kamera als Leuchtursprung). Nur Editor/Szene-Manipulation (Constitution V konform).
    /// Steuerung danach: F = schmelzen, V = Schnee auftragen.
    /// </summary>
    public static class SnowSetup
    {
        private const string SnowTexturePath =
            "Assets/_Project/Prefabs/ImportiertePackaged/SBS - Holiday Texture Pack - 512x512/512x512/Holiday Elements/Holiday_Snow_02-512x512.png";
        private const string MaterialFolder = "Assets/_Project/Materials";
        private const string MaterialPath = "Assets/_Project/Materials/M_SnowMelt.mat";

        [MenuItem("CozySanta/Setup F5 (Schnee-Patch + Lampe)")]
        public static void Setup()
        {
            var shader = Shader.Find("CozySanta/SnowMelt");
            if (shader == null)
            {
                Debug.LogError("[F5Setup] Shader 'CozySanta/SnowMelt' nicht gefunden (kompiliert die Szene zuerst?).");
                return;
            }

            var material = CreateOrLoadMaterial(shader);

            var player = GameObject.Find("Player");
            var cam = Camera.main;
            var basePos = player != null ? player.transform.position : Vector3.zero;
            var forward = cam != null ? cam.transform.forward : Vector3.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

            var patchGo = GameObject.Find("SnowPatch");
            if (patchGo == null)
            {
                patchGo = new GameObject("SnowPatch");
            }

            patchGo.transform.position = new Vector3(basePos.x + (forward.x * 4f), 0f, basePos.z + (forward.z * 4f));
            patchGo.transform.rotation = Quaternion.identity;

            var patch = EnsureComponent<SnowPatch>(patchGo);
            var renderer = patchGo.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (player != null)
            {
                var controller = EnsureComponent<MeltController>(player);
                var serialized = new SerializedObject(controller);
                SetRef(serialized, "viewOrigin", cam != null ? cam.transform : player.transform);
                SetRef(serialized, "patch", patch);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[F5Setup] Kein 'Player' gefunden – MeltController nicht verdrahtet. Bitte manuell ergänzen.");
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[F5Setup] Schnee-Patch + Material + MeltController eingerichtet. Szene speichern (Strg+S), dann Play. " +
                      "F = schmelzen, V = Schnee auftragen. (Patch liegt 4 m vor dem Player auf y=0 – bei Bedarf verschieben.)");
        }

        private static Material CreateOrLoadMaterial(Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                if (!AssetDatabase.IsValidFolder(MaterialFolder))
                {
                    AssetDatabase.CreateFolder("Assets/_Project", "Materials");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SnowTexturePath);
            if (tex != null)
            {
                material.SetTexture("_BaseMap", tex);
            }
            else
            {
                Debug.LogWarning($"[F5Setup] Snow-Textur nicht gefunden: {SnowTexturePath} (Material bleibt weiß).");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void SetRef(SerializedObject serialized, string field, Object value)
        {
            var prop = serialized.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
            else
            {
                Debug.LogWarning($"[F5Setup] Feld '{field}' an {serialized.targetObject.GetType().Name} nicht gefunden.");
            }
        }
    }
}
