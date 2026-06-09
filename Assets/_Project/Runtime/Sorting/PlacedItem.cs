using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Marker an einem im Fach abgelegten Objekt: merkt sich das Besitzer-Fach und die Einlage-Id,
    /// damit der Fadenkreuz-Raycast (der den Brief selbst trifft) die Entnahme gezielt an genau dieses
    /// Fach zurückrouten kann. Wird beim Einlegen gesetzt und bei Entnahme/Abschluss entfernt.
    /// </summary>
    public sealed class PlacedItem : MonoBehaviour
    {
        /// <summary>Fach, in dem dieses Objekt liegt.</summary>
        public SortTargetInteractable Owner { get; private set; }

        /// <summary>Einlage-Id (Instance-Id des Pickup-Components), Schlüssel in der Fach-Buchhaltung.</summary>
        public int Id { get; private set; }

        public void Bind(SortTargetInteractable owner, int id)
        {
            Owner = owner;
            Id = id;
        }
    }
}
