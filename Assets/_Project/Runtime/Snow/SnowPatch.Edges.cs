using System.Collections.Generic;
using UnityEngine;

namespace CozySanta.Runtime.Snow
{
    /// <summary>
    /// Rand- und Nachbarlogik des <see cref="SnowPatch"/> (Apply): bestimmt pro Randpunkt die Höhe der
    /// angrenzenden Fläche und leitet daraus den Höhen-Deckel ab. Solide Nachbarn (Stufen/Terrain) werden
    /// einmalig beim Start erkannt; SnowPatch-Nachbarn werden dynamisch verfolgt – ändert ein Nachbar
    /// seine Schmelzhöhe (Version), passt sich der Randübergang an, sodass keine schwebende Kante entsteht.
    /// </summary>
    public sealed partial class SnowPatch
    {
        private float[] _boundaryTarget = System.Array.Empty<float>();   // Nachbarhöhen-Anteil je Randvertex (0..1)
        private int[] _dynBoundary = System.Array.Empty<int>();          // Rand-Vertizes mit SnowPatch-Nachbar (unique)
        private SnowPatch[] _neighborPatches = System.Array.Empty<SnowPatch>();
        private int[] _neighborVersions = System.Array.Empty<int>();

        /// <summary>Liefert die aktuelle, sichtbare Oberkanten-Höhe (Welt-Y) der Schneefläche am
        /// XZ-Weltpunkt; false, wenn der Punkt außerhalb liegt. Es ist die *gerenderte* Höhe
        /// (Schmelzhöhe, gedeckelt + ausgespart), damit ein Nachbar auch eine an seiner Kante noch
        /// nicht fertig ausgelaufene Rampe übernimmt und die Naht bündig bleibt.</summary>
        public bool TrySurfaceWorldY(Vector3 world, out float worldY)
        {
            worldY = 0f;
            if (_field == null) return false;
            var local = transform.InverseTransformPoint(world);
            var u = (local.x / sizeX) + 0.5f;
            var v = (local.z / sizeZ) + 0.5f;
            var tolU = 0.05f / sizeX; // kleine Toleranz, damit an Ecken geteilte Randpunkte auf allen Patches aufgehen
            var tolV = 0.05f / sizeZ;
            if (u < -tolU || u > 1f + tolU || v < -tolV || v > 1f + tolV) return false;
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
            var h = SampleRenderedBilinear(u, v);
            worldY = transform.TransformPoint(new Vector3((u - 0.5f) * sizeX, h * maxHeight, (v - 0.5f) * sizeZ)).y;
            return true;
        }

        // Sichtbare Oberkanten-Höhe (0..1) einer Zelle: Schmelzhöhe, gedeckelt durch Rand/Nachbar und
        // ausgespart bei Objekten – also genau das, was gerendert wird.
        private float Rendered(int x, int y)
        {
            var i = (y * _field.Resolution) + x;
            return Mathf.Min(_field.HeightAt(x, y), _cap[i]) * _carve[i];
        }

        // Bilinear interpolierte gerenderte Höhe an (u,v) – glatt zwischen den Zellen, damit Nähte
        // ohne Quantisierungs-Stufen aufeinander abgestimmt sind.
        private float SampleRenderedBilinear(float u, float v)
        {
            var r = _field.Resolution;
            var fx = Mathf.Clamp(u * (r - 1), 0f, r - 1);
            var fy = Mathf.Clamp(v * (r - 1), 0f, r - 1);
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            int x1 = Mathf.Min(x0 + 1, r - 1), y1 = Mathf.Min(y0 + 1, r - 1);
            float tx = fx - x0, ty = fy - y0;
            var top = Mathf.Lerp(Rendered(x0, y0), Rendered(x1, y0), tx);
            var bot = Mathf.Lerp(Rendered(x0, y1), Rendered(x1, y1), tx);
            return Mathf.Lerp(top, bot, ty);
        }

        // Initiale Randerkennung (in Start). SyncTransforms stellt sicher, dass neu in Awake angelegte
        // Nachbar-Collider in der Physik-Szene aktuell sind, bevor wir sie antasten.
        private void ShapeEdges()
        {
            Physics.SyncTransforms();
            DetectNeighborEdges();
            BuildCarveMask();
            SyncMesh();
        }

        // Tastet alle vier Ränder ab, bestimmt Nachbar-Patches und berechnet die Deckel neu.
        private void DetectNeighborEdges()
        {
            var r = _field.Resolution;
            _boundaryTarget = new float[r * r];
            var dyn = new HashSet<int>();
            var neighbors = new HashSet<SnowPatch>();

            void Probe(int x, int y, float nx, float nz)
            {
                var gi = (y * r) + x;
                _boundaryTarget[gi] = ProbeNeighbor(x, y, nx, nz, out var nb);
                if (nb == null) return;
                dyn.Add(gi); neighbors.Add(nb);
            }

            for (var x = 0; x < r; x++) { Probe(x, 0, 0f, -1f); Probe(x, r - 1, 0f, 1f); }
            for (var y = 0; y < r; y++) { Probe(0, y, -1f, 0f); Probe(r - 1, y, 1f, 0f); }
            // Diagonale Eck-Antastung: erfasst den dritten (diagonalen) Nachbarn an 4-Patch-Ecken.
            Probe(0, 0, -1f, -1f); Probe(r - 1, 0, 1f, -1f);
            Probe(0, r - 1, -1f, 1f); Probe(r - 1, r - 1, 1f, 1f);

            _dynBoundary = new int[dyn.Count];
            dyn.CopyTo(_dynBoundary);
            _neighborPatches = new SnowPatch[neighbors.Count];
            neighbors.CopyTo(_neighborPatches);
            _neighborVersions = new int[_neighborPatches.Length];

            RecomputeCaps();
        }

        // Spart Schnee dort aus, wo ein Objekt in der Fläche steht: pro Zelle ein Raycast nach unten;
        // ragt dort ein solider (Nicht-Boden-)Collider über den Boden, wird die Zelle als belegt
        // markiert. Ein 1-Ring-Dilation gibt etwas Freiraum ums Objekt. Einmalig beim Start.
        private void BuildCarveMask()
        {
            var r = _field.Resolution;
            var occupied = new bool[r * r];
            for (var y = 0; y < r; y++)
                for (var x = 0; x < r; x++)
                    if (CellOccupied(x, y)) occupied[(y * r) + x] = true;

            // Weicher Übergang: carve = SmoothStep über carveFalloff Meter Abstand zur nächsten
            // belegten Zelle. Objektzellen = 0, ab carveFalloff = 1, dazwischen sanfter Auslauf.
            var fall = Mathf.Max(0.0001f, carveFalloff);
            var cellX = sizeX / (r - 1);
            var cellZ = sizeZ / (r - 1);
            var radX = Mathf.Max(1, Mathf.CeilToInt(fall / cellX));
            var radZ = Mathf.Max(1, Mathf.CeilToInt(fall / cellZ));
            for (var y = 0; y < r; y++)
                for (var x = 0; x < r; x++)
                {
                    var i = (y * r) + x;
                    if (occupied[i]) { _carve[i] = 0f; continue; }
                    var best = float.MaxValue;
                    for (var dy = -radZ; dy <= radZ; dy++)
                        for (var dx = -radX; dx <= radX; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= r || ny >= r || !occupied[(ny * r) + nx]) continue;
                            float ddx = dx * cellX, ddz = dy * cellZ; // echte Weltdistanz bei rechteckigen Zellen
                            var d = Mathf.Sqrt((ddx * ddx) + (ddz * ddz));
                            if (d < best) best = d;
                        }
                    _carve[i] = best >= fall ? 1f : Mathf.SmoothStep(0f, 1f, best / fall);
                }
        }

        // True, wenn an der Zelle ein solides Objekt über dem Boden steht (Boden/Schnee/fremde Trigger zählen nicht).
        private bool CellOccupied(int x, int y)
        {
            var r = _field.Resolution;
            var fx = ((float)x / (r - 1)) - 0.5f;
            var fz = ((float)y / (r - 1)) - 0.5f;
            // Start weit oben, damit der Strahl auch hohe Objekte (z. B. Capsule) von außen/oben trifft
            // statt im Collider zu beginnen (ein Raycast aus dem Inneren meldet keinen Treffer).
            var origin = transform.TransformPoint(new Vector3(fx * sizeX, maxHeight + 100f, fz * sizeZ));
            var hits = Physics.RaycastAll(origin, -transform.up, 300f, ~0, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<SnowPatch>() != null) continue; // eigene/fremde Schneefläche
                if (hit.collider.isTrigger) continue;                                  // AreaZone/Gate u. Ä.
                if (transform.InverseTransformPoint(hit.point).y > 0.05f) return true;  // ragt über den Boden
            }
            return false;
        }

        private int _settleFrames = 4;

        // Pollt die Nachbar-Patches: ändert einer seine Schmelzhöhe, wird der Randübergang neu berechnet.
        private void LateUpdate()
        {
            if (_settleFrames > 0)
            {
                // Erster Frame: Physik ist gesetzt → volle Re-Erkennung der Ränder (behebt frühe
                // Naht-Risse aus Start). Danach ein paar Iterationen, damit sich gegenseitig auf die
                // gerenderte Höhe abgestimmte Nähte sauber einschwingen.
                if (_settleFrames == 4) { Physics.SyncTransforms(); DetectNeighborEdges(); }
                else RecomputeCaps();
                SyncMesh();
                _settleFrames--;
                return;
            }

            if (_neighborPatches.Length == 0) return;
            var changed = false;
            for (var k = 0; k < _neighborPatches.Length; k++)
            {
                var n = _neighborPatches[k];
                if (n == null || n.Version == _neighborVersions[k]) continue;
                _neighborVersions[k] = n.Version;
                changed = true;
            }
            if (changed) { RecomputeCaps(); SyncMesh(); }
        }

        // Aktualisiert dynamische Randhöhen (SnowPatch-Nachbarn) und berechnet den Deckel je Vertex neu.
        private void RecomputeCaps()
        {
            var r = _field.Resolution;
            for (var k = 0; k < _dynBoundary.Length; k++)
            {
                var gi = _dynBoundary[k];
                _boundaryTarget[gi] = SampleNeighborsMinFrac(gi % r, gi / r);
            }
            for (var y = 0; y < r; y++)
            {
                for (var x = 0; x < r; x++)
                {
                    var i = (y * r) + x;
                    _cap[i] = Mathf.Lerp(NearestEdgeTarget(x, y, r, _boundaryTarget), 1f, _edgeBlend[i]);
                }
            }
        }

        // Nachbarhöhe des nächstgelegenen Randes (entlang der dominanten Achse) → Deckel-Zielwert.
        private static float NearestEdgeTarget(int x, int y, int r, float[] target)
        {
            int dL = x, dR = r - 1 - x, dB = y, dT = r - 1 - y;
            var m = Mathf.Min(Mathf.Min(dL, dR), Mathf.Min(dB, dT));
            if (m == dB) return target[x];
            if (m == dT) return target[((r - 1) * r) + x];
            if (m == dL) return target[y * r];
            return target[(y * r) + (r - 1)];
        }

        // Raycast knapp außerhalb des Randpunkts nach unten; liefert die Nachbar-Oberkante als Anteil
        // von maxHeight. Solide Umgebung ODER SnowPatch zählen; fremde Trigger (AreaZone/Gate) und das
        // eigene Objekt werden ignoriert. <paramref name="neighbor"/> = der SnowPatch-Nachbar (für dynamisch).
        private float ProbeNeighbor(int x, int y, float nx, float nz, out SnowPatch neighbor)
        {
            neighbor = null;
            var r = _field.Resolution;
            var fx = ((float)x / (r - 1)) - 0.5f;
            var fz = ((float)y / (r - 1)) - 0.5f;
            const float outward = 0.1f;
            const float startAbove = 1f;

            var origin = transform.TransformPoint(new Vector3(
                (fx * sizeX) + (nx * outward), maxHeight + startAbove, (fz * sizeZ) + (nz * outward)));
            var hits = Physics.RaycastAll(origin, -transform.up, 50f, ~0, QueryTriggerInteraction.Collide);

            var best = 0f;
            var bestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                var sp = hit.collider.GetComponentInParent<SnowPatch>();
                if (hit.collider.isTrigger && sp == null) continue; // fremde Trigger überspringen
                if (hit.distance >= bestDist) continue;
                bestDist = hit.distance;
                best = Mathf.Clamp01(transform.InverseTransformPoint(hit.point).y / maxHeight);
                neighbor = sp; // null bei solider Umgebung (statisch), gesetzt bei SnowPatch (dynamisch)
            }
            return best;
        }

        // Minimum der gerenderten Oberkanten ALLER an diesem Randpunkt anliegenden SnowPatch-Nachbarn
        // (als Anteil von maxHeight, 0..1). So teilen sich an einer 4-Patch-Ecke alle denselben Wert →
        // sauberer Übergang. 1, wenn kein Nachbar den Punkt abdeckt (kein Deckel).
        private float SampleNeighborsMinFrac(int x, int y)
        {
            var r = _field.Resolution;
            var fx = ((float)x / (r - 1)) - 0.5f;
            var fz = ((float)y / (r - 1)) - 0.5f;
            var world = transform.TransformPoint(new Vector3(fx * sizeX, 0f, fz * sizeZ));

            var min = 1f;
            var found = false;
            foreach (var n in _neighborPatches)
            {
                if (n == null || !n.TrySurfaceWorldY(world, out var worldY)) continue;
                var localY = transform.InverseTransformPoint(new Vector3(world.x, worldY, world.z)).y;
                min = Mathf.Min(min, Mathf.Clamp01(localY / maxHeight));
                found = true;
            }
            return found ? min : 1f;
        }
    }
}
