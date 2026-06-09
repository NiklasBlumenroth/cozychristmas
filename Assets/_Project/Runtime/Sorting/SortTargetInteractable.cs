using System.Collections.Generic;
using CozySanta.Core.Sorting;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Ein Sortier-Fach (Apply-Schicht). Hält die reine <see cref="SortTarget"/>-Logik und führt die
    /// Seiteneffekte aus: Einsortieren (Hand→Fach), Entnehmen (Fach→Hand – gezielt per Fadenkreuz über
    /// <see cref="RemoveSpecific"/> oder LIFO als Fallback), Reparenting an den Slot, Lampe und Schließen
    /// bei Vollständigkeit. Eingelegte Objekte behalten ihren Collider (per Fadenkreuz anvisierbar) und
    /// tragen einen <see cref="PlacedItem"/>-Marker, der die Entnahme zurück an dieses Fach routet.
    /// </summary>
    public sealed partial class SortTargetInteractable : MonoBehaviour, IInteractable
    {
        [Header("Sortierregel (editor-authored)")]
        [Tooltip("Akzeptierte Facetten dieses Fachs (muss zur Rahmen-Beschriftung passen).")]
        [SerializeField] private string[] acceptedFacets = new string[0];
        [Tooltip("Soll-Menge korrekter Objekte für den Abschluss.")]
        [SerializeField] private int requiredCount = 3;

        [Header("Anker & Feedback")]
        [Tooltip("Positions-Referenz für eingelegte Objekte (z. B. 'SlotAnker'). Fallback: dieses Transform.")]
        [SerializeField] private Transform slotAnchor;
        [Tooltip("Lampen-GameObject (Emissive-Mesh/Light), standardmäßig inaktiv. Wird bei Abschluss aktiviert.")]
        [SerializeField] private GameObject lamp;
        [Tooltip("Richtung, in der eingelegte Objekte nebeneinander aufgereiht werden – im lokalen Raum des Slots (z. B. (1,0,0) = nach rechts).")]
        [SerializeField] private Vector3 stackDirection = Vector3.right;
        [Tooltip("Mindestabstand zwischen zwei eingelegten Objekten (Meter). Wird automatisch auf die " +
                 "Objektbreite + kleine Lücke angehoben, falls dieser Wert zu klein ist (kein Überlappen).")]
        [SerializeField] private float stackSpacing = 0.03f;
        [Tooltip("Größenfaktor für eingelegte Objekte im Fach (1 = Originalgröße des Objekts, 0.5 = halb " +
                 "so groß, 2 = doppelt). Beeinflusst nur die Anzeige im Fach; beim Entnehmen wird die " +
                 "Originalgröße wiederhergestellt.")]
        [SerializeField] private float placedScale = 1f;
        [SerializeField] private string promptText = "Einsortieren / Entnehmen";

        [Tooltip("Einmaliges Abschluss-Ereignis (Andockpunkt für XP-Vergabe in F6).")]
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();

        private SortTarget _target;
        private readonly Dictionary<int, Component> _placed = new Dictionary<int, Component>();
        // Original-Skalierung je eingelegtem Objekt, damit placedScale beim Entnehmen sauber
        // zurückgesetzt werden kann (kein Aufsummieren bei mehrfachem Ein-/Auslagern).
        private readonly Dictionary<int, Vector3> _originalScale = new Dictionary<int, Vector3>();
        // Zuletzt berechneter Reihenabstand (für die Ghost-Vorschau, siehe TryGetSlotPose).
        private float _lastStep;

        /// <summary>Reiner Fach-Zustand (für Tests/Diagnose).</summary>
        public SortTarget Target => _target;

        /// <summary>Registriert einen Listener für das einmalige Abschluss-Ereignis (Andockpunkt F6-XP).</summary>
        public void AddCompletionListener(UnityEngine.Events.UnityAction listener)
            => onCompleted.AddListener(listener);

        public string PromptText => _target != null && _target.IsClosed ? string.Empty : promptText;

        private void Awake()
        {
            _target = new SortTarget(new SortKey(acceptedFacets), requiredCount);
        }

        /// <summary>
        /// Konfiguriert das Fach zur Laufzeit (datengetriebene Bestückung oder Tests): setzt akzeptierte
        /// Facetten + Soll-Menge neu und optional Slot-/Lampen-Referenz. Baut die Core-Logik frisch auf.
        /// </summary>
        public void Configure(string[] accepted, int required, Transform slot = null, GameObject lampObject = null)
        {
            acceptedFacets = accepted ?? new string[0];
            requiredCount = required;
            if (slot != null)
            {
                slotAnchor = slot;
            }

            if (lampObject != null)
            {
                lamp = lampObject;
            }

            _placed.Clear();
            _originalScale.Clear();
            _target = new SortTarget(new SortKey(acceptedFacets), requiredCount);
        }

        /// <summary>F2-Vertrag; das eigentliche Verhalten läuft über <see cref="HandleInteract"/> (Routing).</summary>
        public void Interact()
        {
            // Bewusst leer: der PlayerInteractionController routet mit PlayerCarry-Kontext.
        }

        /// <summary>
        /// Kontextabhängige Interaktion: trägt der Spieler etwas → Einsortieren; sonst → Entnehmen (LIFO).
        /// Abgeschlossene Fächer sind gesperrt.
        /// </summary>
        public void HandleInteract(PlayerCarry carry)
        {
            if (carry == null || _target == null || _target.IsClosed)
            {
                return;
            }

            if (carry.CarriedCount > 0)
            {
                TryPlaceFromHand(carry);
            }
            else
            {
                TryRemoveToHand(carry);
            }
        }

        /// <summary>Einsortieren: legt das oberste getragene Objekt ins Fach. No-op bei leerer Hand
        /// oder abgeschlossenem Fach. (Andockpunkt für die Maus-Steuerung: Rechtsklick.)</summary>
        public void PlaceFromHand(PlayerCarry carry)
        {
            if (carry == null || _target == null || _target.IsClosed) return;
            TryPlaceFromHand(carry);
        }

        /// <summary>Entnehmen (LIFO-Fallback): nimmt das zuletzt eingelegte Objekt in die Hand. No-op bei
        /// leerem oder abgeschlossenem Fach. Gezieltes Entnehmen läuft über <see cref="RemoveSpecific"/>.</summary>
        public void RemoveToHand(PlayerCarry carry)
        {
            if (carry == null || _target == null || _target.IsClosed) return;
            TryRemoveToHand(carry);
        }

        private void TryPlaceFromHand(PlayerCarry carry)
        {
            if (!carry.TryHandOverTop(out var pickup) || pickup is not Component component)
            {
                return;
            }

            var key = component.TryGetComponent<ISortable>(out var sortable) ? sortable.Key : default;
            var id = component.GetInstanceID();
            if (!_target.TryPlace(id, key))
            {
                // Sollte nicht eintreten (IsClosed oben geprüft); zur Sicherheit zurück in die Hand.
                carry.TryPickup(pickup);
                return;
            }

            _placed[id] = component;
            AttachToSlot(component, _target.Count - 1);

            if (_target.JustCompleted)
            {
                Close();
            }
        }

        private void TryRemoveToHand(PlayerCarry carry)
        {
            if (!_target.TryPeekTop(out var id) || !_placed.TryGetValue(id, out var component))
            {
                return;
            }

            var weight = component.TryGetComponent<IPickup>(out var pickup) ? pickup.Weight : 0f;
            if (pickup == null || !carry.CanCarry(weight))
            {
                return; // Überlast oder nicht aufnehmbar -> Objekt bleibt im Fach.
            }

            if (!_target.TryRemoveTop(out _))
            {
                return;
            }

            ExtractToHand(id, component, pickup, carry);
        }

        private void AttachToSlot(Component component, int index)
        {
            // Bewusst an den Fach-Root parenten (Scale 1), Slot nur als Positions-/Rotationsreferenz,
            // damit ein evtl. 0-skalierter SlotAnker die Einlage nicht unsichtbar macht.
            var reference = slotAnchor != null ? slotAnchor : transform;
            var dir = stackDirection.sqrMagnitude > 0.0001f ? stackDirection.normalized : Vector3.right;
            var worldDir = reference.rotation * dir;

            component.transform.SetParent(transform, worldPositionStays: false);
            component.transform.rotation = reference.rotation;

            // Anzeigegröße zuerst setzen – sie geht in den automatischen Abstand ein.
            ApplyPlacedScale(component);

            // Automatischer, überlappungsfreier Reihenabstand (Mindestabstand vs. Objektbreite + Lücke).
            var step = ComputeStep(component, worldDir);
            component.transform.position = reference.position + (worldDir * (step * index));

            MarkPlaced(component, component.GetInstanceID());
        }

        // Merkt die Originalskalierung und wendet placedScale an (1 = unverändert).
        private void ApplyPlacedScale(Component component)
        {
            if (!(placedScale > 0f) || Mathf.Approximately(placedScale, 1f))
            {
                return;
            }

            var id = component.GetInstanceID();
            if (!_originalScale.ContainsKey(id))
            {
                _originalScale[id] = component.transform.localScale;
            }

            component.transform.localScale = _originalScale[id] * placedScale;
        }

        private void Close()
        {
            if (lamp != null)
            {
                lamp.SetActive(true);
            }

            onCompleted?.Invoke();

            // Eingelegte Objekte bleiben sichtbar, werden aber endgültig gesperrt: Collider aus + Marker weg,
            // damit ein abgeschlossenes Fach nicht mehr per Fadenkreuz angefasst werden kann.
            foreach (var entry in _placed.Values)
            {
                if (entry == null) continue;
                foreach (var collider in entry.GetComponentsInChildren<Collider>(includeInactive: true))
                {
                    collider.enabled = false;
                }

                if (entry.TryGetComponent<PlacedItem>(out var marker)) Destroy(marker);
            }

            // Fach selbst aus der Interaktion nehmen (Opening-Collider am Root deaktivieren).
            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = false;
            }
        }
    }
}
