using CozySanta.Core.Sorting;
using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Editor-authored Sortierdaten eines Objekts: die Facettenwerte bilden den <see cref="SortKey"/>.
    /// Sitzt zusätzlich zum F3-<c>PickupInteractable</c> auf demselben GameObject (Objekt bleibt
    /// aufnehmbar UND sortierbar).
    /// </summary>
    public sealed class Sortable : MonoBehaviour, ISortable
    {
        [Tooltip("Geordnete Facettenwerte, z. B. Kontinent / Farbe / Symbol. Bildet den SortKey.")]
        [SerializeField] private string[] facets = new string[0];

        private SortKey _key;
        private bool _built;

        public SortKey Key
        {
            get
            {
                if (!_built)
                {
                    _key = new SortKey(facets);
                    _built = true;
                }

                return _key;
            }
        }

        /// <summary>Setzt die Facetten zur Laufzeit/aus Editor-Tools und baut den Key neu.</summary>
        public void SetFacets(params string[] values)
        {
            facets = values ?? new string[0];
            _key = new SortKey(facets);
            _built = true;
        }
    }
}
