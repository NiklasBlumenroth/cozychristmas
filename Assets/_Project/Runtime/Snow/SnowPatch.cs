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
    public sealed partial class SnowPatch : MonoBehaviour
    {
        [Tooltip("Zellen pro Achse (Grid ist Resolution × Resolution Vertices).")]
        [SerializeField] private int resolution = 64;
        [Tooltip("Grundriss-Maß in X (Breite) in Metern.")]
        [UnityEngine.Serialization.FormerlySerializedAs("size")]
        [SerializeField] private float sizeX = 8f;
        [Tooltip("Grundriss-Maß in Z (Tiefe) in Metern.")]
        [SerializeField] private float sizeZ = 8f;
        [Tooltip("Volle Schneehöhe in Metern (Dicke der Decke).")]
        [SerializeField] private float maxHeight = 0.5f;
        [Tooltip("Breite des abgeschrägten Randauslaufs in Metern: über diese Strecke fällt die " +
                 "Schneehöhe sanft auf 0 (natürlicher Übergang zum Boden statt senkrechter Kante).")]
        [SerializeField] private float edgeFalloff = 0.5f;
        [Tooltip("Breite des weichen Übergangs (m) um Objekte in der Fläche: über diese Strecke läuft " +
                 "der Schnee zur Objekt-Aussparung hin sanft aus (0 = harte Kante).")]
        [SerializeField] private float carveFalloff = 0.5f;
        [Tooltip("Maximaler Anstieg der Schneeoberfläche als Höhe pro Horizontaldistanz. 1 = 45° " +
                 "(pro 10 cm Strecke max. 10 cm Höhenunterschied). 0 deaktiviert die Begrenzung. " +
                 "Glättet zu steile Nahtstellen zwischen Patches und an Schmelz-/Objektkanten.")]
        [SerializeField] private float maxSlopeRatio = 1f;

        private MeltField _field;
        private Mesh _mesh;
        private Vector3[] _verts;
        private Color[] _colors;
        // Gerenderte Höhe je Vertex in Metern (Schmelzhöhe, gedeckelt, ausgespart) – Arbeitspuffer für
        // die Slope-Limit-Stufe in SyncMesh (wird vor dem Schreiben der Mesh-Höhen geglättet).
        private float[] _renderH = System.Array.Empty<float>();

        // Geometrischer Rand-Blend je Vertex (0 am äußeren Rand … 1 ab edgeFalloff nach innen).
        private float[] _edgeBlend = System.Array.Empty<float>();
        // Höhen-Deckel je Vertex (0..1 × maxHeight): kombiniert Rand-Blend mit der erkannten
        // Nachbarhöhe. Die angezeigte Höhe ist min(Schmelzhöhe, Deckel). In ShapeEdges gesetzt.
        private float[] _cap = System.Array.Empty<float>();
        // Aussparungs-Maske je Vertex (1 = Schnee erlaubt, 0 = von einem Objekt belegt → kein Schnee).
        // Einmalig beim Start aus Collidern in der Fläche ermittelt (siehe BuildCarveMask).
        private float[] _carve = System.Array.Empty<float>();

        /// <summary>Flächen-Fortschritt 0..1 (Anteil freigelegt) aus der Core-Logik.</summary>
        public float Coverage => _field != null ? _field.Coverage : 0f;

        /// <summary>Volle Schneehöhe (für das Zielen auf das Schnee-Volumen).</summary>
        public float AimHeight => maxHeight;

        /// <summary>Zählt bei jeder Schmelz-/Auftrag-Änderung hoch; Nachbar-Patches pollen das,
        /// um ihren Randübergang an die geänderte Oberfläche anzupassen.</summary>
        public int Version { get; private set; }

        private void Awake()
        {
            BuildMesh();
            EnsureAimCollider();
        }

        private void Start()
        {
            // Erst hier: alle Nachbar-Collider (auch SnowPatch-Trigger aus deren Awake) existieren.
            ShapeEdges();
        }

        // Zeigt die Schnee-Ausdehnung (Fläche + Höhe) schon im Edit-Modus an, da das Mesh erst zur
        // Laufzeit generiert wird. Folgt Position/Rotation/Skalierung des Patches.
        private void OnDrawGizmos()
        {
            var prevMatrix = Gizmos.matrix;
            var prevColor = Gizmos.color;

            Gizmos.matrix = transform.localToWorldMatrix;
            var center = new Vector3(0f, maxHeight * 0.5f, 0f);
            var box = new Vector3(sizeX, Mathf.Max(0.02f, maxHeight), sizeZ);

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
            box.size = new Vector3(sizeX, Mathf.Max(0.02f, maxHeight), sizeZ);
        }

        private void BuildMesh()
        {
            var r = Mathf.Max(2, resolution);
            _field = new MeltField(r);

            _verts  = new Vector3[r * r];
            _colors = new Color[r * r];
            _edgeBlend = new float[r * r];
            _cap = new float[r * r];
            _carve = new float[r * r];
            _renderH = new float[r * r];
            var uvs  = new Vector2[r * r];
            var tris = new int[(r - 1) * (r - 1) * 6];

            for (var y = 0; y < r; y++)
            {
                for (var x = 0; x < r; x++)
                {
                    var i = (y * r) + x;
                    var fx = ((float)x / (r - 1)) - 0.5f;
                    var fz = ((float)y / (r - 1)) - 0.5f;
                    var blend = EdgeBlend(fx, fz);
                    _edgeBlend[i] = blend;
                    _cap[i] = 1f;    // sichere Voll-Basis, bis ShapeEdges den echten Deckel setzt
                                     // (ein noch nicht abgetasteter Nachbar wird so nie als „leer" gelesen)
                    _carve[i] = 1f;  // bis BuildCarveMask: überall Schnee erlaubt
                    _verts[i] = new Vector3(fx * sizeX, maxHeight * blend, fz * sizeZ);
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
                    tris[t++] = i; tris[t++] = i + r; tris[t++] = i + 1;
                    tris[t++] = i + 1; tris[t++] = i + r; tris[t++] = i + r + 1;
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

        // Sanfter Geometrie-Blend am Rand: 0 an der äußeren Kante, 1 ab edgeFalloff Metern nach innen.
        // fx/fz liegen in [-0.5, 0.5]; Abstand zur nächsten Kante in Metern bestimmt den Wert.
        private float EdgeBlend(float fx, float fz)
        {
            if (edgeFalloff <= 0f) return 1f;
            var distX = (0.5f - Mathf.Abs(fx)) * sizeX;
            var distZ = (0.5f - Mathf.Abs(fz)) * sizeZ;
            var edgeDist = Mathf.Min(distX, distZ);
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edgeDist / edgeFalloff));
        }

        /// <summary>Senkt die Schneehöhe am Weltpunkt. Punkte bis zu einem Pinselradius außerhalb des
        /// Patches werden mitgenommen, damit ein Lampenkegel sauber über Patch-Grenzen schmilzt;
        /// false, wenn der Kegel den Patch gar nicht berührt.</summary>
        public bool Melt(Vector3 world, float radiusMeters, float strength)
        {
            var local = transform.InverseTransformPoint(world);
            var u = (local.x / sizeX) + 0.5f;
            var v = (local.z / sizeZ) + 0.5f;
            var marginU = radiusMeters / sizeX;
            var marginV = radiusMeters / sizeZ;
            if (u < -marginU || u > 1f + marginU || v < -marginV || v > 1f + marginV)
            {
                return false;
            }

            _field.Melt(u, v, marginU, marginV, strength);
            Version++;
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

            _field.Add(u, v, radiusMeters / sizeX, radiusMeters / sizeZ, strength);
            Version++;
            SyncMesh();
            return true;
        }

        private bool TryWorldToUV(Vector3 world, out float u, out float v)
        {
            var local = transform.InverseTransformPoint(world);
            u = (local.x / sizeX) + 0.5f;
            v = (local.z / sizeZ) + 0.5f;
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
                    var carve = _carve[i];
                    _renderH[i] = Mathf.Min(h, _cap[i]) * carve * maxHeight; // gedeckelt durch Rand/Nachbar, ausgespart bei Objekten
                    _colors[i].r = h * carve; // belegte Zellen → Shader clippt (kein Schnee im Objekt)
                }
            }

            ClampSlope(r); // zu steile Anstiege auf max. maxSlopeRatio (45° bei 1) zurücknehmen

            for (var i = 0; i < _verts.Length; i++)
            {
                _verts[i].y = _renderH[i];
            }

            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        // Begrenzt den Anstieg der gerenderten Oberfläche (in _renderH, Meter): kein Nachbar-Vertex darf
        // höher liegen als der eigene + maxSlopeRatio × Horizontaldistanz. Realisiert als Chamfer-
        // Distanztransform mit zwei Sweeps (vorwärts/rückwärts, je 8 Nachbarn) – das ergibt exakt die
        // untere Hülle h[i] = min_j (h_raw[j] + ratio × dist(i,j)), senkt also nur Spitzen ab und füllt
        // niemals freigeschmolzene/ausgesparte Stellen wieder auf.
        private void ClampSlope(int r)
        {
            if (maxSlopeRatio <= 0f || r < 2)
            {
                return;
            }

            var cellX = sizeX / (r - 1);
            var cellZ = sizeZ / (r - 1);
            var costX = maxSlopeRatio * cellX;
            var costZ = maxSlopeRatio * cellZ;
            var costD = maxSlopeRatio * Mathf.Sqrt((cellX * cellX) + (cellZ * cellZ));

            void Relax(int i, int j, float cost)
            {
                var limit = _renderH[j] + cost;
                if (_renderH[i] > limit)
                {
                    _renderH[i] = limit;
                }
            }

            // Vorwärts-Sweep: bereits verarbeitete Nachbarn (links, oben-links, oben, oben-rechts).
            for (var y = 0; y < r; y++)
            {
                for (var x = 0; x < r; x++)
                {
                    var i = (y * r) + x;
                    if (x > 0) Relax(i, i - 1, costX);
                    if (y > 0) Relax(i, i - r, costZ);
                    if (x > 0 && y > 0) Relax(i, i - r - 1, costD);
                    if (x < r - 1 && y > 0) Relax(i, i - r + 1, costD);
                }
            }

            // Rückwärts-Sweep: rechts, unten-rechts, unten, unten-links → vollständige untere Hülle.
            for (var y = r - 1; y >= 0; y--)
            {
                for (var x = r - 1; x >= 0; x--)
                {
                    var i = (y * r) + x;
                    if (x < r - 1) Relax(i, i + 1, costX);
                    if (y < r - 1) Relax(i, i + r, costZ);
                    if (x < r - 1 && y < r - 1) Relax(i, i + r + 1, costD);
                    if (x > 0 && y < r - 1) Relax(i, i + r - 1, costD);
                }
            }
        }
    }
}
