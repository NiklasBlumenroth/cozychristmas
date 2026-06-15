using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Einmal-Setup für den Brief: baut ein eigenes Box-Mesh mit gezielter UV-Belegung pro Seite
    /// (Vorderseite = Atlas-Oberteil mit Umschlag-Faltung + Stern, Rückseite = Atlas-Unterteil mit
    /// nur Stern, die 4 schmalen Kanten = Papier-Ecke des Atlas) und hängt es an den Brief-Prefab.
    /// Ersetzt das Unity-Standard-Würfelmesh, das auf allen 6 Seiten dieselbe Textur zeigt.
    /// Reine Editor/Asset-Manipulation (Constitution V konform). Textur/Material = T_Brief / M_Brief.
    /// </summary>
    public static class BriefMeshSetup
    {
        private const string MeshFolder = "Assets/_Project/Meshes";
        private const string MeshPath = "Assets/_Project/Meshes/Brief_Mesh.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/Post/Brief.prefab";

        // UV-Punkt in der Papier-Ecke des Atlas (oben links, sauberes Papier) für die schmalen Kanten.
        private static readonly Vector2 PaperUv = new Vector2(0.02f, 0.98f);

        [MenuItem("CozySanta/Items/Brief-Mesh bauen (Vorder-/Rückseite/Papier)")]
        public static void Build()
        {
            var mesh = BuildMesh();

            if (!Directory.Exists(MeshFolder))
                Directory.CreateDirectory(MeshFolder);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                mesh = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, MeshPath);
            }
            AssetDatabase.SaveAssets();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[BriefMeshSetup] Prefab nicht gefunden: {PrefabPath}");
                return;
            }
            var mf = prefab.GetComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.LogError("[BriefMeshSetup] MeshFilter am Brief-Prefab fehlt.");
                return;
            }
            mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            PrefabUtility.SavePrefabAsset(prefab);
            AssetDatabase.Refresh();

            Debug.Log("[BriefMeshSetup] Brief-Mesh gebaut und an Prefab gehängt. " +
                      "Material M_Brief (Atlas T_Brief) zeigt jetzt Vorder-/Rückseite getrennt.");
        }

        private static Mesh BuildMesh()
        {
            const float h = 0.5f; // Einheits-Box; der Prefab skaliert auf 0.2 x 0.1 x 0.01.

            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();

            // Vorderseite (+Z): Atlas-Oberteil v in [0.5, 1.0]
            AddFace(verts, uvs, tris,
                new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h),
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f));

            // Rückseite (-Z): Atlas-Unterteil v in [0.0, 0.5]
            AddFace(verts, uvs, tris,
                new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f));

            // +X Kante (Papier)
            AddFace(verts, uvs, tris,
                new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h),
                PaperUv, PaperUv, PaperUv, PaperUv);

            // -X Kante (Papier)
            AddFace(verts, uvs, tris,
                new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h),
                PaperUv, PaperUv, PaperUv, PaperUv);

            // +Y Kante (Papier)
            AddFace(verts, uvs, tris,
                new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h),
                PaperUv, PaperUv, PaperUv, PaperUv);

            // -Y Kante (Papier)
            AddFace(verts, uvs, tris,
                new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, -h, h), new Vector3(-h, -h, h),
                PaperUv, PaperUv, PaperUv, PaperUv);

            var mesh = new Mesh { name = "Brief_Mesh" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Fügt eine Quad-Fläche hinzu. Reihenfolge bl,br,tr,tl = von außen gesehen (CCW),
        // Dreiecke werden vorderseitig (Unity-Uhrzeigersinn) gewickelt.
        private static void AddFace(
            System.Collections.Generic.List<Vector3> verts,
            System.Collections.Generic.List<Vector2> uvs,
            System.Collections.Generic.List<int> tris,
            Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
            Vector2 uvBl, Vector2 uvBr, Vector2 uvTr, Vector2 uvTl)
        {
            int b = verts.Count;
            verts.Add(bl); verts.Add(br); verts.Add(tr); verts.Add(tl);
            uvs.Add(uvBl); uvs.Add(uvBr); uvs.Add(uvTr); uvs.Add(uvTl);
            tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
        }
    }
}
