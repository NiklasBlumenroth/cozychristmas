using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup: macht die Kugeln eines „decorative_lights"-Meshes leuchten. Zerlegt das
    /// zusammengeführte „Sphere"-Mesh in einzelne Kugeln (zusammenhängende Mesh-Inseln via Union-Find),
    /// setzt pro Kugel (optional nur jede N-te) ein warmes Point-Light an deren Mittelpunkt und legt ein
    /// emissives Material aufs Sphere-Mesh (sichtbares Glühen + Bloom). Auswahl: das decorative_lights-Objekt.
    /// </summary>
    public static class DecorativeLightsSetup
    {
        // --- Einstellungen (bei Bedarf hier anpassen) ---
        private static readonly Color LightColor = new Color(1f, 0.83f, 0.6f); // wie die Laterne
        private const float LightIntensity = 2.5f;
        private const float LightRange = 1.2f;
        private const int LightEvery = 1;     // nur jede N-te Kugel bekommt ein Licht (Performance)
        private const int MaxLights = 32;     // Obergrenze an Echtzeit-Lichtern
        private const int MinIslandVerts = 8; // kleinere Inseln (Artefakte) ignorieren

        private static readonly Color EmissionColor = new Color(1f, 0.85f, 0.55f) * 3f; // HDR > Bloom-Threshold
        private const string MaterialPath = "Assets/_Project/Prefabs/post/DecorativeBulb_Emissive.mat";
        private const string ContainerName = "BulbLights";

        [MenuItem("CozySanta/Setup Decorative Lights (Kugeln beleuchten)")]
        public static void Setup()
        {
            var root = Selection.activeGameObject;
            if (root == null)
            {
                Debug.LogError("[DecoLights] Bitte das decorative_lights-Objekt in der Hierarchie auswählen.");
                return;
            }

            var sphere = FindSphere(root);
            if (sphere == null || sphere.sharedMesh == null)
            {
                Debug.LogError("[DecoLights] Kein Mesh-Objekt 'Sphere' unter der Auswahl gefunden.");
                return;
            }

            ApplyEmissiveMaterial(sphere.GetComponent<MeshRenderer>());

            var centroids = ClusterIslands(sphere.sharedMesh, sphere.transform);
            if (centroids.Count == 0)
            {
                Debug.LogWarning("[DecoLights] Keine Kugel-Inseln gefunden (Mesh nicht lesbar?).");
                return;
            }

            PlaceLights(sphere.transform, centroids);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
        }

        private static MeshFilter FindSphere(GameObject root)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
            {
                var meshName = mf.sharedMesh != null ? mf.sharedMesh.name.ToLowerInvariant() : string.Empty;
                if (mf.name.ToLowerInvariant().Contains("sphere") || meshName.Contains("sphere"))
                {
                    return mf;
                }
            }

            return null;
        }

        // Union-Find über die Dreiecke: jede zusammenhängende Mesh-Insel = eine Kugel.
        private static List<Vector3> ClusterIslands(Mesh mesh, Transform meshTransform)
        {
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var parent = new int[verts.Length];
            for (var i = 0; i < parent.Length; i++) parent[i] = i;

            for (var t = 0; t + 2 < tris.Length; t += 3)
            {
                Union(parent, tris[t], tris[t + 1]);
                Union(parent, tris[t + 1], tris[t + 2]);
            }

            var sum = new Dictionary<int, Vector3>();
            var count = new Dictionary<int, int>();
            for (var i = 0; i < verts.Length; i++)
            {
                var r = Find(parent, i);
                sum.TryGetValue(r, out var s);
                count.TryGetValue(r, out var c);
                sum[r] = s + verts[i];
                count[r] = c + 1;
            }

            var centroids = new List<Vector3>();
            foreach (var kv in count)
            {
                if (kv.Value < MinIslandVerts) continue;
                var localCentroid = sum[kv.Key] / kv.Value;
                centroids.Add(meshTransform.TransformPoint(localCentroid));
            }

            // Deterministische Reihenfolge (für stabiles LightEvery).
            centroids.Sort((a, b) =>
            {
                var dx = a.x.CompareTo(b.x);
                if (dx != 0) return dx;
                var dy = a.y.CompareTo(b.y);
                return dy != 0 ? dy : a.z.CompareTo(b.z);
            });
            return centroids;
        }

        private static void PlaceLights(Transform sphereTransform, List<Vector3> centroids)
        {
            var old = sphereTransform.Find(ContainerName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var container = new GameObject(ContainerName);
            container.transform.SetParent(sphereTransform, worldPositionStays: false);

            var placed = 0;
            for (var i = 0; i < centroids.Count && placed < MaxLights; i += Mathf.Max(1, LightEvery))
            {
                var go = new GameObject($"BulbLight_{placed}");
                go.transform.SetParent(container.transform, worldPositionStays: true);
                go.transform.position = centroids[i];

                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = LightColor;
                light.intensity = LightIntensity;
                light.range = LightRange;
                light.shadows = LightShadows.None;
                placed++;
            }

            Debug.Log($"[DecoLights] {centroids.Count} Kugeln erkannt, {placed} Point-Lights gesetzt " +
                      $"(LightEvery={LightEvery}, Max={MaxLights}). Emissives Material zugewiesen. Szene speichern (Strg+S).");
        }

        private static void ApplyEmissiveMaterial(MeshRenderer renderer)
        {
            if (renderer == null) return;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader) { name = "DecorativeBulb_Emissive" };
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", EmissionColor);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.8f, 1f));
            EditorUtility.SetDirty(mat);

            renderer.sharedMaterial = mat;
        }

        private static int Find(int[] parent, int a)
        {
            while (parent[a] != a)
            {
                parent[a] = parent[parent[a]];
                a = parent[a];
            }

            return a;
        }

        private static void Union(int[] parent, int a, int b)
        {
            var ra = Find(parent, a);
            var rb = Find(parent, b);
            if (ra != rb) parent[ra] = rb;
        }
    }
}
