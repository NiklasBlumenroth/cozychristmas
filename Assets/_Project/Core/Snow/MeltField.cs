using System;

namespace CozySanta.Core.Snow
{
    /// <summary>
    /// Reines, testbares Höhenfeld eines Schnee-Bereichs (ohne Unity). Ein Zell-Grid hält die
    /// Schneehöhe je Zelle (0..1); 0 = freigelegt (Boden sichtbar). Schmelzen senkt, Auftragen hebt
    /// die Höhe per weichem Pinsel. <see cref="Coverage"/> liefert den Flächen-Fortschritt
    /// (Anteil freigelegter Zellen) – Autorität für XP/Aufgaben (F6/F7).
    /// </summary>
    public sealed class MeltField
    {
        private const float Epsilon = 0.001f;

        private readonly float[] _heights;
        private int _clearedCells;

        public MeltField(int resolution)
        {
            Resolution = resolution < 1 ? 1 : resolution;
            _heights = new float[Resolution * Resolution];
            for (var i = 0; i < _heights.Length; i++)
            {
                _heights[i] = 1f;
            }
        }

        /// <summary>Zellen pro Achse (Grid ist Resolution × Resolution).</summary>
        public int Resolution { get; }

        /// <summary>Gesamtzahl der Zellen.</summary>
        public int CellCount => _heights.Length;

        /// <summary>Anteil freigelegter Zellen 0..1 (Höhe ≈ 0).</summary>
        public float Coverage => CellCount == 0 ? 0f : (float)_clearedCells / CellCount;

        /// <summary>Flächen-Fortschritt in Prozent (0..100).</summary>
        public float CoveragePercent => Coverage * 100f;

        /// <summary>Schneehöhe (0..1) an der Zelle.</summary>
        public float HeightAt(int x, int y) => _heights[Index(x, y)];

        /// <summary>Senkt die Höhe um <paramref name="strength"/> in einem weichen Pinsel um (u,v).</summary>
        public void Melt(float u, float v, float radius, float strength) => Apply(u, v, radius, -strength);

        /// <summary>Hebt die Höhe um <paramref name="strength"/> in einem weichen Pinsel um (u,v).</summary>
        public void Add(float u, float v, float radius, float strength) => Apply(u, v, radius, strength);

        private void Apply(float u, float v, float radius, float delta)
        {
            if (radius <= 0f || delta == 0f)
            {
                return;
            }

            var last = Resolution - 1;
            var cx = u * last;
            var cy = v * last;
            var r = radius * last;
            if (r <= 0f)
            {
                return;
            }

            var minX = (int)Math.Floor(cx - r);
            var maxX = (int)Math.Ceiling(cx + r);
            var minY = (int)Math.Floor(cy - r);
            var maxY = (int)Math.Ceiling(cy + r);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (x < 0 || y < 0 || x >= Resolution || y >= Resolution)
                    {
                        continue;
                    }

                    float dx = x - cx, dy = y - cy;
                    var d = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (d > r)
                    {
                        continue;
                    }

                    var falloff = 1f - (d / r); // weicher Pinsel: Mitte stark, Rand 0
                    UpdateCell(Index(x, y), delta * falloff);
                }
            }
        }

        private void UpdateCell(int idx, float change)
        {
            if (change == 0f)
            {
                return;
            }

            var before = _heights[idx];
            var after = before + change;
            if (after < 0f)
            {
                after = 0f;
            }
            else if (after > 1f)
            {
                after = 1f;
            }

            if (after == before)
            {
                return;
            }

            var wasCleared = before <= Epsilon;
            var nowCleared = after <= Epsilon;
            if (!wasCleared && nowCleared)
            {
                _clearedCells++;
            }
            else if (wasCleared && !nowCleared)
            {
                _clearedCells--;
            }

            _heights[idx] = after;
        }

        private int Index(int x, int y) => (Clamp(y) * Resolution) + Clamp(x);

        private int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > Resolution - 1 ? Resolution - 1 : value;
        }
    }
}
