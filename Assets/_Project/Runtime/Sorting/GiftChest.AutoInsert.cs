using CozySanta.Core.Sorting;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Items;
using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Auto-Einsortieren in die Truhe (Fähigkeit A, Option C: von oben einwerfen). Öffnet bei Bedarf den
    /// Deckel, prüft über eine Einwurf-Zone (oberer Bereich des Innenvolumens), ob noch Platz ist, und
    /// lässt das getragene Geschenk an einen Punkt oben im Volumen fliegen, wo es per Physik in den Stapel
    /// fällt. Validierung/Verriegelung bleiben unverändert beim manuellen Schließen.
    /// </summary>
    public sealed partial class GiftChest
    {
        // Anteil der Volumenhöhe, der als Einwurf-Zone für die Voll-Prüfung dient (oben).
        private const float EntryZoneHeightFraction = 0.25f;
        private const float AutoInsertFlightDuration = 0.6f;

        /// <summary>True, wenn diese Sorte hier hineingehört (deckungsgleicher SortKey).</summary>
        public bool Accepts(SortKey key) => !key.IsEmpty && Accepted.Matches(key);

        /// <summary>True, wenn das Auto-Einsortieren möglich ist: nicht verriegelt, passende Sorte, Platz frei.</summary>
        public bool CanAutoInsert(SortKey key)
            => !_locked && insideVolume != null && Accepts(key) && HasDropSpace();

        /// <summary>Öffnet den Deckel, falls geschlossen (und nicht verriegelt/animierend).</summary>
        public void EnsureOpen()
        {
            if (_locked || _animating || _isOpen) return;
            Open();
        }

        /// <summary>
        /// Wirft das oberste getragene Objekt in die Truhe (Fähigkeit A). Validiert Sorte + Platz erneut,
        /// öffnet den Deckel, übernimmt das Item aus der Hand und lässt es oben ins Volumen fliegen; bei der
        /// Landung wird es physisch (fällt in den Stapel). False, wenn nicht möglich (keine Ladung verbrauchen).
        /// </summary>
        public bool TryAutoInsert(PlayerCarry carry)
        {
            if (carry == null || _locked || insideVolume == null) return false;
            if (!carry.TryPeekTopComponent(out var top)) return false;

            var key = top.TryGetComponent<ISortable>(out var sortable) ? sortable.Key : default;
            if (!Accepts(key) || !HasDropSpace()) return false;

            if (!carry.TryHandOverTop(out var pickup) || pickup is not Component component) return false;

            EnsureOpen();

            component.transform.SetParent(null, worldPositionStays: true);
            var target = DropPoint();
            var rot = Random.rotationUniform;
            CarriedItemFlight.For(component).BeginToWorld(target, rot, AutoInsertFlightDuration,
                sweep: false, onLanded: () => Release(component));
            return true;
        }

        // Macht das eingeworfene Item bei der Landung wieder physisch, sodass es in den Stapel fällt und ruht.
        private static void Release(Component component)
        {
            foreach (var collider in component.GetComponentsInChildren<Collider>(includeInactive: true))
                collider.enabled = true;

            if (component.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = false;
                body.useGravity = true;
            }

            if (component.TryGetComponent<SettlingBody>(out var settling))
                settling.BeginSettling();
        }

        // Zielpunkt für den Einwurf: oben im Innenvolumen, leicht zufällig gestreut.
        private Vector3 DropPoint()
        {
            var b = insideVolume.bounds;
            var x = b.center.x + Random.Range(-1f, 1f) * b.extents.x * 0.5f;
            var z = b.center.z + Random.Range(-1f, 1f) * b.extents.z * 0.5f;
            var y = b.max.y - b.extents.y * EntryZoneHeightFraction * 0.5f;
            return new Vector3(x, y, z);
        }

        // Voll-Prüfung: ist die obere Einwurf-Zone bereits durch Items belegt, gilt die Truhe als voll.
        private bool HasDropSpace()
        {
            var b = insideVolume.bounds;
            var h = b.size.y * EntryZoneHeightFraction;
            var center = new Vector3(b.center.x, b.max.y - h * 0.5f, b.center.z);
            var half = new Vector3(b.extents.x * 0.9f, h * 0.5f, b.extents.z * 0.9f);

            var hits = Physics.OverlapBox(center, half, Quaternion.identity, itemMask, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit != null && hit.GetComponentInParent<Sortable>() != null) return false; // belegt
            }

            return true;
        }
    }
}
