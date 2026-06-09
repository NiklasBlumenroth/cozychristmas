using CozySanta.Runtime.Carry;
using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Slot-Geometrie- und Entnahme-Teil des Fachs (Apply-Schicht): Belegungsstand, Slot-Pose für die
    /// Ghost-Vorschau, automatischer Reihenabstand und das gezielte Entnehmen eines einzelnen Briefs.
    /// Ausgelagert, um die Kern-Interaktion in <see cref="SortTargetInteractable"/> schlank zu halten
    /// (300-Zeilen-Limit / fachliche Trennung).
    /// </summary>
    public sealed partial class SortTargetInteractable
    {
        // Sicherheitslücke zwischen zwei eingelegten Objekten (Meter), zusätzlich zur Objektbreite.
        private const float SlotGap = 0.01f;

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
            // Auto-Abstand des letzten Platzierens nutzen (vor dem ersten Einlegen reicht stackSpacing,
            // da index 0 ohnehin keinen Versatz hat).
            var step = _lastStep > 0f ? _lastStep : stackSpacing;
            position = reference.position + (reference.rotation * (dir * (step * _target.Count)));
            rotation = reference.rotation;
            return true;
        }

        /// <summary>Gezieltes Entnehmen (Fadenkreuz): nimmt genau den über den <see cref="PlacedItem"/>-Marker
        /// referenzierten Brief in die Hand. No-op bei gesperrtem Fach, Überlast oder unbekannter Id.</summary>
        public void RemoveSpecific(PlacedItem marker, PlayerCarry carry)
        {
            if (carry == null || _target == null || _target.IsClosed || marker == null)
            {
                return;
            }

            var id = marker.Id;
            if (!_placed.TryGetValue(id, out var component))
            {
                return;
            }

            var weight = component.TryGetComponent<IPickup>(out var pickup) ? pickup.Weight : 0f;
            if (pickup == null || !carry.CanCarry(weight))
            {
                return; // Überlast oder nicht aufnehmbar -> Brief bleibt im Fach.
            }

            if (!_target.TryRemove(id))
            {
                return;
            }

            ExtractToHand(id, component, pickup, carry);
        }

        // Gemeinsamer Entnahme-Seiteneffekt (gezielt + LIFO): Buchhaltung räumen, Anzeigegröße
        // zurücksetzen, Marker entfernen und das Objekt an die Hand übergeben.
        private void ExtractToHand(int id, Component component, IPickup pickup, PlayerCarry carry)
        {
            _placed.Remove(id);

            if (_originalScale.TryGetValue(id, out var original))
            {
                component.transform.localScale = original;
                _originalScale.Remove(id);
            }

            if (component.TryGetComponent<PlacedItem>(out var marker))
            {
                Destroy(marker);
            }

            // TryPickup setzt die Physik wieder auf „getragen" (Collider aus, kinematisch).
            carry.TryPickup(pickup);
        }

        // Eingelegtes Objekt: Collider bleiben AKTIV (per Fadenkreuz anvisierbar), Objekt ruht
        // kinematisch im Fach; Marker verweist zur Entnahme zurück auf dieses Fach.
        private void MarkPlaced(Component component, int id)
        {
            foreach (var collider in component.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                collider.enabled = true;
            }

            if (component.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            var marker = component.GetComponent<PlacedItem>();
            if (marker == null)
            {
                marker = component.gameObject.AddComponent<PlacedItem>();
            }

            marker.Bind(this, id);
        }

        // Reihenabstand = max(Mindestabstand, Objektbreite entlang der Reihenrichtung + Lücke).
        private float ComputeStep(Component component, Vector3 worldDir)
        {
            var size = 0f;
            var renderers = component.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                size = Mathf.Abs(Vector3.Dot(bounds.size, worldDir.normalized));
            }

            _lastStep = Mathf.Max(stackSpacing, size + SlotGap);
            return _lastStep;
        }
    }
}
