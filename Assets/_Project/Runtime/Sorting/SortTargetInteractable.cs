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
    /// Seiteneffekte aus: Einsortieren (Hand→Fach), Entnehmen (Fach→Hand, LIFO), Reparenting an den
    /// Slot, Deaktivieren der eingelegten Collider, Lampe und Schließen bei Vollständigkeit. Ist ein
    /// <see cref="IInteractable"/>; das Routing (Player) ruft <see cref="HandleInteract"/> mit dem
    /// <see cref="PlayerCarry"/> auf. Kein <c>IPickup</c> → fällt nicht in den Aufnehmen-Pfad.
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
        [Tooltip("Abstand zwischen zwei eingelegten Objekten entlang der Stapelrichtung (Meter).")]
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

        /// <summary>Reiner Fach-Zustand (für Tests/Diagnose).</summary>
        public SortTarget Target => _target;

        /// <summary>Registriert einen Listener für das einmalige Abschluss-Ereignis (Andockpunkt F6-XP).</summary>
        public void AddCompletionListener(UnityEngine.Events.UnityAction listener)
            => onCompleted.AddListener(listener);

        public string PromptText => _target != null && _target.IsClosed ? string.Empty : promptText;

        private void Awake()
        {
            _target = new SortTarget(new SortKey(acceptedFacets), requiredCount);
            Debug.Log($"[SortDbg] Awake Fach '{name}': requiredCount={requiredCount}, " +
                      $"accepted=[{string.Join(",", acceptedFacets)}], placedScale={placedScale}, " +
                      $"stackSpacing={stackSpacing}, slotAnchor={(slotAnchor != null ? slotAnchor.name : "<null/Fallback transform>")}", this);
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
            Debug.Log($"[SortDbg] PlaceFromHand '{name}': carry={(carry != null ? carry.CarriedCount.ToString() : "null")}, " +
                      $"IsClosed={_target?.IsClosed}, Count={_target?.Count}, Correct={_target?.CorrectCount}, Wrong={_target?.WrongCount}", this);
            if (carry == null || _target == null || _target.IsClosed)
            {
                Debug.Log($"[SortDbg] PlaceFromHand '{name}' ABGEBROCHEN (carry null / target null / IsClosed).", this);
                return;
            }
            TryPlaceFromHand(carry);
        }

        /// <summary>Entnehmen: nimmt ein Objekt aus dem Fach in die Hand (aktuell LIFO). No-op bei
        /// leerem oder abgeschlossenem Fach. (Andockpunkt für die Maus-Steuerung: Linksklick.)</summary>
        public void RemoveToHand(PlayerCarry carry)
        {
            Debug.Log($"[SortDbg] RemoveToHand '{name}': carry={(carry != null ? carry.CarriedCount.ToString() : "null")}, " +
                      $"IsClosed={_target?.IsClosed}, Count={_target?.Count}, _placed={_placed.Count}", this);
            if (carry == null || _target == null || _target.IsClosed)
            {
                Debug.Log($"[SortDbg] RemoveToHand '{name}' ABGEBROCHEN (carry null / target null / IsClosed).", this);
                return;
            }
            TryRemoveToHand(carry);
        }

        private void TryPlaceFromHand(PlayerCarry carry)
        {
            if (!carry.TryHandOverTop(out var pickup) || pickup is not Component component)
            {
                Debug.Log($"[SortDbg] TryPlaceFromHand '{name}': KEIN Objekt aus der Hand erhalten (TryHandOverTop=false oder kein Component).", this);
                return;
            }

            var hasSortable = component.TryGetComponent<ISortable>(out var sortable);
            var key = hasSortable ? sortable.Key : default;
            var id = component.GetInstanceID();
            var correct = _target.Classify(key);
            Debug.Log($"[SortDbg] TryPlaceFromHand '{name}': Objekt='{component.name}' id={id}, " +
                      $"hasSortable={hasSortable}, key=[{key}], classify(correct)={correct}", this);

            if (!_target.TryPlace(id, key))
            {
                // Sollte nicht eintreten (IsClosed oben geprüft); zur Sicherheit zurück in die Hand.
                Debug.LogWarning($"[SortDbg] TryPlaceFromHand '{name}': TryPlace=false (IsClosed={_target.IsClosed}) -> zurück in die Hand.", this);
                carry.TryPickup(pickup);
                return;
            }

            _placed[id] = component;
            AttachToSlot(component, _target.Count - 1);
            Debug.Log($"[SortDbg] TryPlaceFromHand '{name}': PLATZIERT id={id}. " +
                      $"Count={_target.Count}, Correct={_target.CorrectCount}, Wrong={_target.WrongCount}, " +
                      $"_placed={_placed.Count}, JustCompleted={_target.JustCompleted}, IsClosed={_target.IsClosed}", this);

            if (_target.JustCompleted)
            {
                Close();
            }
        }

        private void TryRemoveToHand(PlayerCarry carry)
        {
            var peeked = _target.TryPeekTop(out var id);
            Component component = null;
            var inPlaced = peeked && _placed.TryGetValue(id, out component);
            if (!peeked || !inPlaced)
            {
                Debug.LogWarning($"[SortDbg] TryRemoveToHand '{name}': nichts entnehmbar. " +
                                 $"TryPeekTop={peeked}, peekId={id}, inPlaced={inPlaced}, " +
                                 $"Count={_target.Count}, _placed={_placed.Count}. " +
                                 (peeked && !inPlaced ? "DIVERGENZ: Eintrag im SortTarget, aber kein Component in _placed!" : ""), this);
                return;
            }

            var weight = component.TryGetComponent<IPickup>(out var pickup) ? pickup.Weight : 0f;
            if (pickup == null || !carry.CanCarry(weight))
            {
                Debug.Log($"[SortDbg] TryRemoveToHand '{name}': bleibt im Fach. id={id}, " +
                          $"pickup={(pickup != null)}, weight={weight}, CanCarry={carry.CanCarry(weight)}.", this);
                return; // Überlast oder nicht aufnehmbar -> Objekt bleibt im Fach.
            }

            _target.TryRemoveTop(out _);
            _placed.Remove(id);
            Debug.Log($"[SortDbg] TryRemoveToHand '{name}': ENTNOMMEN id={id} '{component.name}'. " +
                      $"Count={_target.Count}, _placed={_placed.Count}.", this);

            // Anzeigegröße zurücksetzen, bevor das Objekt zurück in die Hand geht.
            if (_originalScale.TryGetValue(id, out var original))
            {
                component.transform.localScale = original;
                _originalScale.Remove(id);
            }

            // Interaktions-Collider wieder aktivieren, bevor das Objekt zurück in die Hand geht – sonst
            // bliebe es (Collider am Kind) nach späterem Ablegen in der Welt nicht mehr aufnehmbar.
            SetInteractionPhysics(component, active: true);
            carry.TryPickup(pickup);
        }

        private void AttachToSlot(Component component, int index)
        {
            // Bewusst an den Fach-Root parenten (Scale 1), Slot nur als Positions-/Rotationsreferenz,
            // damit ein evtl. 0-skalierter SlotAnker die Einlage nicht unsichtbar macht.
            var reference = slotAnchor != null ? slotAnchor : transform;
            var dir = stackDirection.sqrMagnitude > 0.0001f ? stackDirection.normalized : Vector3.right;
            // Versatz im lokalen Raum des Slots -> Reihung in der gewählten Richtung (nebeneinander).
            var offset = reference.rotation * (dir * (stackSpacing * index));
            component.transform.SetParent(transform, worldPositionStays: false);
            component.transform.position = reference.position + offset;
            component.transform.rotation = reference.rotation;
            Debug.Log($"[SortDbg] AttachToSlot '{name}': '{component.name}' index={index}, " +
                      $"refPos={reference.position}, offset={offset}, worldPos={component.transform.position}, " +
                      $"lossyScale={component.transform.lossyScale}, activeInHierarchy={component.gameObject.activeInHierarchy}", this);

            // Anzeigegröße im Fach: Originalskalierung merken und mit placedScale skalieren.
            if (placedScale > 0f && !Mathf.Approximately(placedScale, 1f))
            {
                var id = component.GetInstanceID();
                if (!_originalScale.ContainsKey(id))
                {
                    _originalScale[id] = component.transform.localScale;
                }
                component.transform.localScale = _originalScale[id] * placedScale;
            }

            SetInteractionPhysics(component, active: false);
        }

        private void Close()
        {
            Debug.Log($"[SortDbg] Close '{name}': Fach GESCHLOSSEN/GESPERRT. " +
                      $"Count={_target.Count}, Correct={_target.CorrectCount}, _placed={_placed.Count}. " +
                      "Root-Collider werden deaktiviert -> Fach nicht mehr fokussierbar.", this);
            if (lamp != null)
            {
                lamp.SetActive(true);
            }

            onCompleted?.Invoke();

            // Eingelegte Objekte bleiben sichtbar im Fach liegen (kein Destroy). Das Fach ist über
            // IsClosed gesperrt, daher werden sie nicht mehr entnommen/verschoben.

            // Fach aus der Interaktion nehmen (Opening-Collider am Root deaktivieren).
            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = false;
            }
        }

        private static void SetInteractionPhysics(Component component, bool active)
        {
            foreach (var collider in component.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                collider.enabled = active;
            }

            if (component.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = !active;
                body.useGravity = active;
            }
        }
    }
}
