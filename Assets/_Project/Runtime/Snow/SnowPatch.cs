using CozySanta.Core.Snow;
using UnityEngine;

namespace CozySanta.Runtime.Snow
{
    /// <summary>
    /// Eine volumetrische Schnee-Fläche (Apply-Schicht): generiert ein Grid-Mesh über dem Boden und
    /// spiegelt das reine Core-<see cref="MeltField"/> visuell – Vertex-Höhe = Schneehöhe, die Höhe
    /// liegt zusätzlich im Vertex-Color.r (für den Shader-Clip an freigelegten Stellen). Schmelzen/
    /// Auftragen läuft über das Core-Feld (Decide), das Mesh wird danach synchronisiert (Apply).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SnowPatch : MonoBehaviour
    {
        [Tooltip("Zellen pro Achse (Grid ist Resolution × Resolution Vertices).")]
        [SerializeField] private int resolution = 64;
        [Tooltip("Kantenlänge des Patches in Metern (quadratisch).")]
        [SerializeField] private float size = 8f;
        [Tooltip("Volle Schneehöhe in Metern (Dicke der Decke).")]
        [SerializeField] private float maxHeight = 0.5f;

        private MeltField _field;
        private Mesh _mesh;
        private Vector3[] _verts;
        private Color[] _colors;

        /// <summary>Flächen-Fortschritt 0..1 (Anteil freigelegt) aus der Core-Logik.</summary>
        public float Coverage => _field != null ? _field.Coverage : 0f;

        /// <summary>Volle Schneehöhe (für das Zielen auf das Schnee-Volumen).</summary>
        public float AimHeight => maxHeight;

        private void Awake()
        {
            BuildMesh();
            EnsureAimCollider();
        }

        // Zeigt die Schnee-Ausdehnung (Fläche + Höhe) schon im Edit-Modus an, da das Mesh erst zur
        // Laufzeit generiert wird. Folgt Position/Rotation/Skalierung des Patches.
        private void OnDrawGizmos()
        {
            var prevMatrix = Gizmos.matrix;
            var prevColor = Gizmos.color;

            Gizmos.matrix = transform.localToWorldMatrix;
            var center = new Vector3(0f, maxHeight * 0.5f, 0f);
            var box = new Vector3(size, Mathf.Max(0.02f, maxHeight), size);

            Gizmos.color = new Color(0.55f, 0.8f, 1f, 0.12f);
            Gizmos.DrawCube(center, box);
            Gizmos.color = new Color(0.55f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(center, box);

            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;
        }

        private void EnsureAimCollider()
        {
            // Trigger-Box über dem Patch, damit der Blick-Raycast das Schnee-Volumen trifft
            // (blockiert die Bewegung nicht). Bleibt als volles Volumen stehen (Zielhilfe, kein Physik-Schnee).
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
            box.center = new Vector3(0f, maxHeight * 0.5f, 0f);
            box.size = new Vector3(size, Mathf.Max(0.02f, maxHeight), size);
        }

        private void BuildMesh()
        {
            var r = Mathf.Max(2, resolution);
            _field = new MeltField(r);
            _verts = new Vector3[r * r];
            _colors = new Color[r * r];
            var uvs = new Vector2[r * r];
            var tris = new int[(r - 1) * (r - 1) * 6];

            for (var y = 0; y < r; y++)
            {
                for (var x = 0; x < r; x++)
                {
                    var i = (y * r) + x;
                    var fx = ((float)x / (r - 1)) - 0.5f;
                    var fz = ((float)y / (r - 1)) - 0.5f;
                    _verts[i] = new Vector3(fx * size, maxHeight, fz * size);
                    uvs[i] = new Vector2((float)x / (r - 1), (float)y / (r - 1));
                    _colors[i] = new Color(1f, 1f, 1f, 1f); // height in r-channel
                }
            }

            var t = 0;
            for (var y = 0; y < r - 1; y++)
            {
                for (var x = 0; x < r - 1; x++)
                {
                    var i = (y * r) + x;
                    tris[t++] = i;
                    tris[t++] = i + r;
                    tris[t++] = i + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + r;
                    tris[t++] = i + r + 1;
                }
            }

            _mesh = new Mesh { name = "SnowPatchMesh" };
            _mesh.indexFormat = r * r > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            _mesh.MarkDynamic();
            _mesh.SetVertices(_verts);
            _mesh.SetUVs(0, uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(tris, 0);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        /// <summary>Senkt die Schneehöhe am Weltpunkt; false, wenn der Punkt außerhalb des Patches liegt.</summary>
        public bool Melt(Vector3 world, float radiusMeters, float strength)
        {
            if (!TryWorldToUV(world, out var u, out var v))
            {
                return false;
            }

            _field.Melt(u, v, radiusMeters / size, strength);
            SyncMesh();
            return true;
        }

        /// <summary>Hebt die Schneehöhe am Weltpunkt wieder an (Dev-Auftrag).</summary>
        public bool AddSnow(Vector3 world, float radiusMeters, float strength)
        {
            if (!TryWorldToUV(world, out var u, out var v))
            {
                return false;
            }

            _field.Add(u, v, radiusMeters / size, strength);
            SyncMesh();
            return true;
        }

        private bool TryWorldToUV(Vector3 world, out float u, out float v)
        {
            var local = transform.InverseTransformPoint(world);
            u = (local.x / size) + 0.5f;
            v = (local.z / size) + 0.5f;
            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
        }

        private void SyncMesh()
        {
            var r = _field.Resolution;
            for (var y = 0; y < r; y++)
            {
                for (var x = 0; x < r; x++)
                {
                    var i = (y * r) + x;
                    var h = _field.HeightAt(x, y);
                    _verts[i].y = h * maxHeight;
                    _colors[i].r = h;
                }
            }

            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
