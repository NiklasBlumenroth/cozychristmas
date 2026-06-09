using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Slot-Geometrie-Teil des Fachs (Apply-Schicht): liefert Belegungsstand und die Pose des
    /// nächsten freien Einlage-Slots. Andockpunkt für die Ghost-Vorschau, ohne die Kern-Interaktion
    /// in <see cref="SortTargetInteractable"/> aufzublähen (300-Zeilen-Limit / fachliche Trennung).
    /// </summary>
    public sealed partial class SortTargetInteractable
    {
        /// <summary>True, wenn das Fach noch ein Objekt aufnehmen kann (offen + unter Soll-Menge).</summary>
        public bool HasFreeSlot => _target != null && _target.HasFreeSlot;

        /// <summary>
        /// Liefert Position/Rotation/Skalierungsfaktor für den NÄCHSTEN Einlage-Slot – dieselbe
        /// Berechnung wie <see cref="AttachToSlot"/> mit index = aktueller Count. False, wenn kein
        /// Platz frei ist.
        /// </summary>
        public bool TryGetSlotPose(out Vector3 position, out Quaternion rotation, out float scaleMultiplier)
        {
            position = default;
            rotation = default;
            scaleMultiplier = (placedScale > 0f && !Mathf.Approximately(placedScale, 1f)) ? placedScale : 1f;
            if (_target == null || !_target.HasFreeSlot)
            {
                return false;
            }

            var reference = slotAnchor != null ? slotAnchor : transform;
            var dir = stackDirection.sqrMagnitude > 0.0001f ? stackDirection.normalized : Vector3.right;
            var offset = reference.rotation * (dir * (stackSpacing * _target.Count));
            position = reference.position + offset;
            rotation = reference.rotation;
            return true;
        }
    }
}
