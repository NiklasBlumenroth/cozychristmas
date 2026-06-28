using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CozySanta.Editor
{
    /// <summary>
    /// Erzeugt das Karton-Aussehen für das Postpaket – seitenrichtig wie ein echter Versandkarton (RSC):
    /// Ober-/Unterseite (Deckel/Boden) tragen die mittige Klappen-Naht mit Paketband, die vier Seitenwände
    /// sind glatte Pappe mit Knick nur an den Kanten. Da Unitys Standard-Würfel jede Seite gleich mappt,
    /// braucht es dafür – analog zu <c>Brief_Mesh</c> – ein eigenes Box-Mesh (<c>Paket_Mesh</c>) mit pro
    /// Seite unterschiedlichen UVs in eine zweizonige Atlas-Textur (<c>T_PaketKarton.png</c>): linke Hälfte
    /// = Wand, rechte Hälfte = Deckel. Material <c>M_PaketKarton.mat</c> (URP/Lit, matt) + Mesh werden an
    /// den <c>Cube</c> von <c>Paket.prefab</c> gehängt und ersetzen das falsche rote Material.
    /// Reine Editor/Asset-Optik (dokumentierte Nicht-Unit-Ausnahme analog SnowMelt/NightSky).
    /// </summary>
    public static class PaketCardboardSetup
    {
        private const string TexPath    = "Assets/_Project/Textures/T_PaketKarton.png";
        private const string MatPath    = "Assets/_Project/Materials/M_PaketKarton.mat";
        private const string MeshPath   = "Assets/_Project/Meshes/Paket_Mesh.asset";
        private const string BasePrefab = "Assets/_Project/Prefabs/Paket.prefab";
        private const int    Size       = 512;

        // Kraftpapier-Ton + abgeleitete Farben.
        private static readonly Color Cardboard = new Color(0.74f, 0.57f, 0.36f);
        private static readonly Color Crease    = new Color(0.50f, 0.37f, 0.22f); // dunkle Falt-Kante
        private static readonly Color Tape      = new Color(0.83f, 0.72f, 0.52f); // helleres Paketband
        private static readonly Color Seam      = new Color(0.42f, 0.31f, 0.18f); // Naht unter dem Band

        // Atlas-Zonen (u): [0, 0.5] = Wand, [0.5, 1] = Deckel/Boden.
        private const float CreaseEdge = 0.07f; // Anteil der Fläche, der zur Kante hin abdunkelt
        private const float TapeHalf   = 0.10f; // halbe Bandbreite (in v, nur Deckel)
        private const float SeamHalf   = 0.008f; // halbe Nahtlinie (in v, nur Deckel)

        [MenuItem("CozySanta/Items/Paket-Kartonmaterial bauen (Falten, seitenrichtig) + an Basis-Prefab")]
        public static void Build()
        {
            var mat  = BuildTextureAndMaterial();
            if (mat == null) return;
            var mesh = BuildMesh();

            AssignToBasePrefab(mat, mesh);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PaketKarton] Karton-Textur + Box-Mesh + Material gebaut und an Paket.prefab (Cube) gehängt: " +
                      "Naht/Tape nur oben/unten, Wände glatt. Danach 'Paket-Varianten erzeugen …' ausführen.");
        }

        private static Material BuildTextureAndMaterial()
        {
            EnsureFolder(Path.GetDirectoryName(TexPath).Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(MatPath).Replace('\\', '/'));

            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, mipChain: true);
            var px = new Color[Size * Size];

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var isLid = x >= Size / 2;             // rechte Hälfte = Deckel/Boden
                    var fu = (x % (Size / 2)) / (float)(Size / 2 - 1); // 0..1 innerhalb der Zone
                    var fv = y / (float)(Size - 1);                    // 0..1 über die Höhe

                    var c = Cardboard;

                    // Feine Papierfaser (deterministisches Rauschen).
                    var n = (Hash(x, y) - 0.5f) * 0.06f;
                    c.r += n; c.g += n; c.b += n;

                    // Knick-Kanten: zur Flächenkante hin abdunkeln (gilt für beide Zonen → jede Würfelkante knickt).
                    var d = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
                    if (d < CreaseEdge)
                    {
                        c = Color.Lerp(c, Crease, (1f - d / CreaseEdge) * 0.85f);
                    }

                    // Nur Deckel/Boden: Paketband entlang der Mitte + dunkle Naht darunter.
                    if (isLid)
                    {
                        var dv = Mathf.Abs(fv - 0.5f);
                        if (dv <= TapeHalf) c = Color.Lerp(c, Tape, 0.8f);
                        if (dv <= SeamHalf) c = Color.Lerp(c, Seam, 0.8f);
                    }

                    px[y * Size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(TexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexPath, ImportAssetOptions.ForceSynchronousImport);
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[PaketKarton] Shader 'Universal Render Pipeline/Lit' nicht gefunden (URP aktiv?).");
                return null;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            var isNew = mat == null;
            if (isNew) mat = new Material(shader) { name = "M_PaketKarton" };
            mat.shader = shader;
            mat.SetTexture("_BaseMap", imported);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.12f); // matt wie Pappe
            if (isNew) AssetDatabase.CreateAsset(mat, MatPath);
            else EditorUtility.SetDirty(mat);
            return mat;
        }

        // Einheits-Box; UVs: Wände (±X/±Z) -> linke Zone [0,0.5], Deckel/Boden (±Y) -> rechte Zone [0.5,1].
        private static Mesh BuildMesh()
        {
            EnsureFolder(Path.GetDirectoryName(MeshPath).Replace('\\', '/'));
            const float h = 0.5f;

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            // Wand-Zone-Eckpunkte (bl,br,tr,tl) und Deckel-Zone.
            Vector2 wbl = new(0f, 0f), wbr = new(0.5f, 0f), wtr = new(0.5f, 1f), wtl = new(0f, 1f);
            Vector2 lbl = new(0.5f, 0f), lbr = new(1f, 0f), ltr = new(1f, 1f), ltl = new(0.5f, 1f);

            // +Z / -Z / +X / -X = Wände
            AddFace(verts, uvs, tris, new(-h,-h,h), new(h,-h,h), new(h,h,h), new(-h,h,h), wbl, wbr, wtr, wtl);
            AddFace(verts, uvs, tris, new(h,-h,-h), new(-h,-h,-h), new(-h,h,-h), new(h,h,-h), wbl, wbr, wtr, wtl);
            AddFace(verts, uvs, tris, new(h,-h,h), new(h,-h,-h), new(h,h,-h), new(h,h,h), wbl, wbr, wtr, wtl);
            AddFace(verts, uvs, tris, new(-h,-h,-h), new(-h,-h,h), new(-h,h,h), new(-h,h,-h), wbl, wbr, wtr, wtl);

            // +Y / -Y = Deckel/Boden (u entlang X, v entlang Z -> Naht bei v=0.5 läuft entlang der langen Achse)
            AddFace(verts, uvs, tris, new(-h,h,h), new(h,h,h), new(h,h,-h), new(-h,h,-h), lbl, lbr, ltr, ltl);
            AddFace(verts, uvs, tris, new(-h,-h,-h), new(h,-h,-h), new(h,-h,h), new(-h,-h,h), lbl, lbr, ltr, ltl);

            var mesh = new Mesh { name = "Paket_Mesh" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, MeshPath);
            return mesh;
        }

        private static void AssignToBasePrefab(Material mat, Mesh mesh)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[PaketKarton] Basis-Prefab fehlt: {BasePrefab} (Material/Mesh trotzdem gebaut).");
                return;
            }

            foreach (var mr in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                if (mr.gameObject.name == "Symbol") continue; // Symbol-Quads nicht überschreiben
                mr.sharedMaterial = mat;
                if (mr.TryGetComponent<MeshFilter>(out var mf)) mf.sharedMesh = mesh;
            }
            PrefabUtility.SavePrefabAsset(prefab);
        }

        // Quad-Fläche bl,br,tr,tl (von außen CCW), vorderseitig gewickelt – wie BriefMeshSetup.
        private static void AddFace(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
            Vector2 uvBl, Vector2 uvBr, Vector2 uvTr, Vector2 uvTl)
        {
            var b = verts.Count;
            verts.Add(bl); verts.Add(br); verts.Add(tr); verts.Add(tl);
            uvs.Add(uvBl); uvs.Add(uvBr); uvs.Add(uvTr); uvs.Add(uvTl);
            tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
        }

        // Deterministisches 0..1-Rauschen aus Pixelkoordinaten.
        private static float Hash(int x, int y)
        {
            var h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
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
