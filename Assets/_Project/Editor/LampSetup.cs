using CozySanta.Runtime.Snow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für die Lampen-Optik: erzeugt zwei Materialien (Gehäuse-Metall + emissiver Funke), baut
    /// ein Lampen-Rig (Platzhalter-Funke, Kern-/Kegel-Licht, Funkel-Partikel) unter der Kamera, hängt
    /// <see cref="LampVisuals"/> daran und verdrahtet es mit dem <see cref="MeltController"/> am Player.
    /// Nur Editor/Szene-Manipulation (Constitution V konform). Das KI-Mesh wird später unter „Lampe" gehängt
    /// und der Funke-Renderer im <see cref="LampVisuals"/> auf das echte Kugel-Mesh umgesetzt.
    /// Bloom (URP Post-Processing) bewusst nicht per Code – ein 2-Klick-Schritt, siehe Log.
    /// </summary>
    public static class LampSetup
    {
        private const string MaterialFolder = "Assets/_Project/Materials";
        private const string ShellMaterialPath = "Assets/_Project/Materials/M_LampGehaeuse.mat";
        private const string OrbMaterialPath = "Assets/_Project/Materials/M_LampFunke.mat";

        private static readonly Color WarmColor = new Color(1f, 0.70f, 0.30f);
        private static readonly Color ColdColor = new Color(0.35f, 0.55f, 1f);

        [MenuItem("CozySanta/Setup Lampe (Material, Lichter, Partikel)")]
        public static void Setup()
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                Debug.LogError("[LampSetup] Shader 'Universal Render Pipeline/Lit' nicht gefunden (URP aktiv?).");
                return;
            }

            EnsureMaterialFolder();
            var shellMat = CreateShellMaterial(litShader);
            var orbMat = CreateOrbMaterial(litShader);

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[LampSetup] Keine MainCamera gefunden – Rig nicht gebaut. Kamera taggen und erneut ausführen.");
                return;
            }

            var rig = BuildRig(cam.transform, orbMat);
            WireVisuals(rig, orbMat);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[LampSetup] Lampe eingerichtet: Materialien (M_LampGehaeuse, M_LampFunke), Rig unter der Kamera " +
                      "(Funke + Kern-/Kegel-Licht + Funkeln) und LampVisuals mit dem MeltController verdrahtet.\n" +
                      "Nächste Schritte: (1) KI-Mesh unter 'Lampe' hängen, Gehäuse-Teil = M_LampGehaeuse, Kugel = M_LampFunke; " +
                      "Funke-Platzhalter ausblenden/löschen und LampVisuals.orbRenderer auf das echte Kugel-Mesh setzen. " +
                      "(2) Für das weiche Glühen: Global Volume + Bloom-Override im URP-Profil aktivieren (HDR-Emission liegt schon über 1).");
        }

        private static GameObject BuildRig(Transform camera, Material orbMat)
        {
            var rig = FindOrCreateChild(camera, "Lampe");
            rig.transform.localPosition = new Vector3(0.28f, -0.22f, 0.55f);
            rig.transform.localRotation = Quaternion.Euler(0f, -8f, 6f);

            // Platzhalter-Funke (wird später durchs echte Kugel-Mesh ersetzt).
            var orb = FindOrCreateChild(rig.transform, "Funke");
            EnsureSphereMesh(orb, orbMat);
            orb.transform.localPosition = Vector3.zero;
            orb.transform.localScale = Vector3.one * 0.06f;
            var orbCollider = orb.GetComponent<Collider>();
            if (orbCollider != null) Object.DestroyImmediate(orbCollider);

            // Kern-Licht in der Kuppel.
            var core = FindOrCreateChild(rig.transform, "KernLicht");
            core.transform.localPosition = Vector3.zero;
            var coreLight = EnsureComponent<Light>(core);
            coreLight.type = LightType.Point;
            coreLight.color = WarmColor;
            coreLight.intensity = 1.2f;
            coreLight.range = 4f;
            coreLight.shadows = LightShadows.None;

            // Kegel-Licht nach vorne (Schmelz-Kegel, koppelt an LampCone).
            var cone = FindOrCreateChild(rig.transform, "Kegel");
            cone.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            cone.transform.localRotation = Quaternion.identity;
            var coneLight = EnsureComponent<Light>(cone);
            coneLight.type = LightType.Spot;
            coneLight.color = WarmColor;
            coneLight.intensity = 2f;
            coneLight.range = 7f;
            coneLight.spotAngle = 45f;
            coneLight.innerSpotAngle = 25f;
            coneLight.shadows = LightShadows.None;

            // Funkel-Partikel am Funke.
            var sparksGo = FindOrCreateChild(rig.transform, "Funkeln");
            sparksGo.transform.localPosition = Vector3.zero;
            ConfigureSparks(EnsureComponent<ParticleSystem>(sparksGo));

            return rig;
        }

        private static void WireVisuals(GameObject rig, Material orbMat)
        {
            var visuals = EnsureComponent<LampVisuals>(rig);
            var meltController = Object.FindFirstObjectByType<MeltController>();
            if (meltController == null)
            {
                Debug.LogWarning("[LampSetup] Kein MeltController in der Szene – bitte LampVisuals.lamp manuell setzen (erst Setup F5 ausführen).");
            }

            var orbRenderer = rig.transform.Find("Funke")?.GetComponent<Renderer>();
            var coreLight = rig.transform.Find("KernLicht")?.GetComponent<Light>();
            var coneLight = rig.transform.Find("Kegel")?.GetComponent<Light>();
            var sparks = rig.transform.Find("Funkeln")?.GetComponent<ParticleSystem>();

            var so = new SerializedObject(visuals);
            SetRef(so, "lamp", meltController);
            SetRef(so, "orbRenderer", orbRenderer);
            SetRef(so, "coreLight", coreLight);
            SetRef(so, "coneLight", coneLight);
            SetRef(so, "sparks", sparks);
            so.FindProperty("warmColor").colorValue = WarmColor;
            so.FindProperty("coldColor").colorValue = ColdColor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSparks(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = WarmColor;
            main.startSize = 0.015f;
            main.startSpeed = 0.12f;
            main.startLifetime = 1.2f;
            main.gravityModifier = -0.02f;
            main.maxParticles = 40;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 4f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var sprite = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
                if (sprite != null) renderer.sharedMaterial = sprite;
            }
        }

        private static Material CreateShellMaterial(Shader shader)
        {
            var mat = LoadOrCreate(shader, ShellMaterialPath, "M_LampGehaeuse");
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.72f, 0.52f, 0.22f, 1f));
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.85f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrbMaterial(Shader shader)
        {
            var mat = LoadOrCreate(shader, OrbMaterialPath, "M_LampFunke");
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.7f, 1f));
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", WarmColor * 2.5f); // HDR > 1 für Bloom; LampVisuals überschreibt zur Laufzeit
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreate(Shader shader, string path, string name)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            return mat;
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }
        }

        private static void EnsureSphereMesh(GameObject go, Material mat)
        {
            var filter = EnsureComponent<MeshFilter>(go);
            if (filter.sharedMesh == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                filter.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(temp);
            }

            EnsureComponent<MeshRenderer>(go).sharedMaterial = mat;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.objectReferenceValue = value;
            else Debug.LogWarning($"[LampSetup] Feld '{field}' an {so.targetObject.GetType().Name} nicht gefunden.");
        }
    }
}
