using System;

namespace CozySanta.Core.Sorting
{
    /// <summary>
    /// Datengetriebener Kategorie-Deskriptor: eine geordnete Folge benannter Facettenwerte
    /// (z. B. ["Europe","Rot","Stern"]). Reiner Wertetyp mit positionsbezogener Wertegleichheit;
    /// keine Unity-Abhängigkeit. Skaliert von wenigen Grey-Box-Kategorien bis zu den vollen
    /// Konzeptmengen ohne Code-Änderung.
    /// </summary>
    public readonly struct SortKey : IEquatable<SortKey>
    {
        private readonly string[] _facets;

        public SortKey(params string[] facets)
        {
            _facets = facets ?? Array.Empty<string>();
        }

        /// <summary>Anzahl der Facetten dieses Schlüssels.</summary>
        public int Length => _facets?.Length ?? 0;

        /// <summary>True, wenn keine Facette gesetzt ist (z. B. nicht sortierbares Objekt).</summary>
        public bool IsEmpty => Length == 0;

        /// <summary>Liefert den Facettenwert an <paramref name="index"/> (oder null außerhalb der Grenzen).</summary>
        public string Facet(int index)
        {
            if (_facets == null || index < 0 || index >= _facets.Length)
            {
                return null;
            }

            return _facets[index];
        }

        /// <summary>True, wenn beide Schlüssel wertgleich sind (gleiche Facettenanzahl und -werte, positionsbezogen).</summary>
        public bool Matches(SortKey other) => Equals(other);

        public bool Equals(SortKey other)
        {
            var a = _facets ?? Array.Empty<string>();
            var b = other._facets ?? Array.Empty<string>();
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is SortKey other && Equals(other);

        public override int GetHashCode()
        {
            var facets = _facets ?? Array.Empty<string>();
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < facets.Length; i++)
                {
                    hash = (hash * 31) + (facets[i]?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }

        public override string ToString() => _facets == null ? string.Empty : string.Join("/", _facets);

        public static bool operator ==(SortKey left, SortKey right) => left.Equals(right);

        public static bool operator !=(SortKey left, SortKey right) => !left.Equals(right);
    }
}
