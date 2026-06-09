using CozySanta.Runtime.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Interaktionsgriff einer einzelnen Raster-Spalte eines <see cref="SortTargetInteractable"/>:
    /// ein BoxCollider, der die ganze Tiefe der Spalte (x,y) abdeckt. Der Fadenkreuz-Raycast trifft
    /// diesen Collider; das Routing legt darüber in den hintersten freien Slot der Spalte ein bzw.
    /// entnimmt aus dem vordersten belegten Slot. Wird zur Laufzeit von der Fach-Logik erzeugt.
    /// </summary>
    public sealed class SlotColumn : MonoBehaviour, IInteractable
    {
        private Collider _collider;

        /// <summary>Besitzer-Fach (Raster-Container).</summary>
        public SortTargetInteractable Owner { get; private set; }

        /// <summary>Spaltenindex (x).</summary>
        public int X { get; private set; }

        /// <summary>Reihenindex (y).</summary>
        public int Y { get; private set; }

        public string PromptText => Owner != null ? Owner.PromptText : string.Empty;

        public void Bind(SortTargetInteractable owner, int x, int y)
        {
            Owner = owner;
            X = x;
            Y = y;
            _collider = GetComponent<Collider>();
        }

        /// <summary>Schaltet den Spalten-Collider (z. B. Sperren bei Fach-Abschluss).</summary>
        public void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }

        /// <summary>F2-Vertrag; das Verhalten läuft über das Routing im PlayerInteractionController.</summary>
        public void Interact()
        {
        }
    }
}
