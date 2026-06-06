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
        private int[] _dynBoundary = System.Array.Empty<int>();          // Rand-Vertizes mit SnowPatch-Nachbar
        private SnowPatch[] _dynBoundaryPatch = System.Array.Empty<SnowPatch>();
        private SnowPatch[] _neighborPatches = System.Array.Empty<SnowPatch>();
        private int[] _neighborVersions = System.Array.Empty<int>();

        /// <summary>Liefert die aktuelle Schmelzhöhe (Welt-Y) der Schneefläche am XZ-Weltpunkt;
        /// false, wenn der Punkt außerhalb liegt. Liefert die reine Feld-Höhe (ohne Rand-Deckel),
        /// damit Nachbarn der tatsächlichen Schneemenge folgen – nicht der Rand-Optik.</summary>
        public bool TrySurfaceWorldY(Vector3 world, out float worldY)
        {
            worldY = 0f;
            if (_field == null || !TryWorldToUV(world, out var u, out var v)) return false;
            var r = _field.Resolution;
            var cx = Mathf.Clamp(Mathf.RoundToInt(u * (r - 1)), 0, r - 1);
            var cy = Mathf.Clamp(Mathf.RoundToInt(v * (r - 1)), 0, r - 1);
            var h = _field.HeightAt(cx, cy);
            worldY = transform.TransformPoint(new Vector3((u - 0.5f) * size, h * maxHeight, (v - 0.5f) * size)).y;
            return true;
        }

        // Initiale Randerkennung (in Start, wenn alle Nachbar-Collider existieren).
        private void ShapeEdges()
        {
            var r = _field.Resolution;
            _boundaryTarget = new float[r * r];
            var dyn = new List<int>();
            var dynPatch = new List<SnowPatch>();
            var neighbors = new HashSet<SnowPatch>();

            void Probe(int x, int y, float nx, float nz)
            {
                var gi = (y * r) + x;
                _boundaryTarget[gi] = ProbeNeighbor(x, y, nx, nz, out var nb);
                if (nb == null) return;
                dyn.Add(gi); dynPatch.Add(nb); neighbors.Add(nb);
            }

            for (var x = 0; x < r; x++) { Probe(x, 0, 0f, -1f); Probe(x, r - 1, 0f, 1f); }
            for (var y = 0; y < r; y++) { Probe(0, y, -1f, 0f); Probe(r - 1, y, 1f, 0f); }

            _dynBoundary = dyn.ToArray();
            _dynBoundaryPatch = dynPatch.ToArray();
            _neighborPatches = new SnowPatch[neighbors.Count];
            neighbors.CopyTo(_neighborPatches);
            _neighborVersions = new int[_neighborPatches.Length];

            RecomputeCaps();
            BuildCarveMask();
            SyncMesh();
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
            var cell = size / (r - 1);
            var rad = Mathf.Max(1, Mathf.CeilToInt(fall / cell));
            for (var y = 0; y < r; y++)
                for (var x = 0; x < r; x++)
                {
                    var i = (y * r) + x;
                    if (occupied[i]) { _carve[i] = 0f; continue; }
                    var best = float.MaxValue;
                    for (var dy = -rad; dy <= rad; dy++)
                        for (var dx = -rad; dx <= rad; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= r || ny >= r || !occupied[(ny * r) + nx]) continue;
                            var d = Mathf.Sqrt((dx * dx) + (dy * dy)) * cell;
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
            var origin = transform.TransformPoint(new Vector3(fx * size, maxHeight + 100f, fz * size));
            var hits = Physics.RaycastAll(origin, -transform.up, 300f, ~0, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<SnowPatch>() != null) continue; // eigene/fremde Schneefläche
                if (hit.collider.isTrigger) continue;                                  // AreaZone/Gate u. Ä.
                if (transform.InverseTransformPoint(hit.point).y > 0.05f) return true;  // ragt über den Boden
            }
            return false;
        }

        // Pollt die Nachbar-Patches: ändert einer seine Schmelzhöhe, wird der Randübergang neu berechnet.
        private void LateUpdate()
        {
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
                _boundaryTarget[gi] = SampleNeighborFrac(gi % r, gi / r, _dynBoundaryPatch[k]);
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
                (fx * size) + (nx * outward), maxHeight + startAbove, (fz * size) + (nz * outward)));
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

        // Aktuelle Oberkante eines SnowPatch-Nachbarn am Randpunkt, als Anteil von maxHeight (0..1).
        private float SampleNeighborFrac(int x, int y, SnowPatch nb)
        {
            if (nb == null) return 0f;
            var r = _field.Resolution;
            var fx = ((float)x / (r - 1)) - 0.5f;
            var fz = ((float)y / (r - 1)) - 0.5f;
            var nx = x == 0 ? -1f : (x == r - 1 ? 1f : 0f);
            var nz = y == 0 ? -1f : (y == r - 1 ? 1f : 0f);
            const float outward = 0.1f;

            var world = transform.TransformPoint(new Vector3(
                (fx * size) + (nx * outward), 0f, (fz * size) + (nz * outward)));
            if (!nb.TrySurfaceWorldY(world, out var worldY)) return 0f;
            var localY = transform.InverseTransformPoint(new Vector3(world.x, worldY, world.z)).y;
            return Mathf.Clamp01(localY / maxHeight);
        }
    }
}
