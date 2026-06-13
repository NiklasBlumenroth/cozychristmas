using System.Collections.Generic;
using CozySanta.Core.Sorting;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>Füllverhalten eines Fachs.</summary>
    public enum SortFillMode
    {
        /// <summary>Jede (x,y)-Spalte wird einzeln anvisiert; nur die Tiefe (z) füllt automatisch.</summary>
        PerColumn,
        /// <summary>Jede x-Spalte = ein Behälter (Spalte per Zielen wählen); y (Höhe) und z (Tiefe)
        /// füllen automatisch: unten→oben, hinten→vorne.</summary>
        Container
    }

    /// <summary>
    /// Generischer 3D-Slot-Container (Apply-Schicht). Erzeugt aus <see cref="gridSize"/> ein Raster
    /// aus Spalten (x), Reihen (y) und Tiefe (z) und je Spalte einen <see cref="SlotColumn"/>-Collider
    /// als Fadenkreuz-Ziel. Einlegen füllt den HINTERSTEN freien Slot der anvisierten Spalte, Entnehmen
    /// nimmt den VORDERSTEN belegten – so lassen sich auch Lager-Kisten hintereinander stapeln. JEDES Item
    /// ist einlegbar; <see cref="acceptedFacets"/> dient nur der Validierung – <see cref="SortTarget"/> zählt
    /// korrekt/falsch und schließt erst ab, wenn genug korrekte und keine falschen Objekte enthalten sind.
    /// </summary>
    public sealed partial class SortTargetInteractable : MonoBehaviour, IInteractable
    {
        [Header("Sortierregel (editor-authored)")]
        [Tooltip("Soll-Code dieses Fachs. Jedes Item ist einlegbar; der Code dient nur der Validierung " +
                 "(korrekt/falsch) und dem Abschluss.")]
        [SerializeField] private string[] acceptedFacets = new string[0];
        [Tooltip("Soll-Menge korrekter Objekte für den Abschluss.")]
        [SerializeField] private int requiredCount = 25;
        [Tooltip("PerColumn: jede (x,y)-Spalte einzeln anvisieren (nur Tiefe automatisch). " +
                 "Container: pro x-Spalte ein Behälter – x wählst du durchs Zielen, y (Höhe) und z (Tiefe) " +
                 "füllen automatisch (unten→oben, hinten→vorne).")]
        [SerializeField] private SortFillMode fillMode = SortFillMode.PerColumn;

        [Header("Slot-Raster")]
        [Tooltip("Rasergröße: x = Spalten (Breite), y = Reihen (Höhe), z = Tiefe (hintereinander).")]
        [SerializeField] private Vector3Int gridSize = new Vector3Int(3, 1, 1);
        [Tooltip("Abstand zwischen den Slot-Mittelpunkten je Achse (Meter, im lokalen Raum des Ankers).")]
        [SerializeField] private Vector3 cellSpacing = new Vector3(0.12f, 0.12f, 0.12f);
        [Tooltip("Größe des anvisierbaren Spalten-Colliders: x,y = Querschnitt (Trefferfläche), z = Tiefe " +
                 "pro Slot. Bei (0,0,0) wird automatisch cellSpacing verwendet.")]
        [SerializeField] private Vector3 colliderSize = Vector3.zero;
        [Tooltip("Positions-/Rotationsreferenz für das Raster (Mitte vorne). Fallback: dieses Transform.")]
        [SerializeField] private Transform slotAnchor;
        [Tooltip("Größenfaktor für eingelegte Objekte im Fach (1 = Originalgröße).")]
        [SerializeField] private float placedScale = 1f;
        [Tooltip("Zusätzliche Drehung (Euler-Winkel) der EINGELEGTEN Objekte relativ zur Anker-Ausrichtung. " +
                 "Ändert nur die Orientierung der Objekte/Ghosts, nicht die Reihenrichtung des Rasters.")]
        [SerializeField] private Vector3 placedEuler = Vector3.zero;

        [Header("Feedback")]
        [SerializeField] private GameObject lamp;
        [SerializeField] private string promptText = "Einsortieren / Entnehmen";
        [Tooltip("Einmaliges Abschluss-Ereignis (Andockpunkt für XP-Vergabe in F6).")]
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();

        private SortTarget _target;
        private Component[,,] _grid;
        private SlotColumn[,] _columns;
        private readonly Dictionary<int, Vector3> _originalScale = new Dictionary<int, Vector3>();

        /// <summary>Reiner Fach-Zustand (für Tests/Diagnose).</summary>
        public SortTarget Target => _target;

        /// <summary>Registriert einen Listener für das einmalige Abschluss-Ereignis (Andockpunkt F6-XP).</summary>
        public void AddCompletionListener(UnityEngine.Events.UnityAction listener)
            => onCompleted.AddListener(listener);

        public string PromptText => _target != null && _target.IsClosed ? string.Empty : promptText;

        private void Awake()
        {
            BuildContainer();
        }

        /// <summary>
        /// Konfiguriert das Fach zur Laufzeit/aus Tests neu: akzeptierte Facetten, Soll-Menge und
        /// optional Anker/Lampe; baut Core-Logik, Raster und Spalten-Collider frisch auf.
        /// </summary>
        public void Configure(string[] accepted, int required, Transform slot = null, GameObject lampObject = null,
            Vector3Int? grid = null)
        {
            acceptedFacets = accepted ?? new string[0];
            requiredCount = required;
            if (slot != null) slotAnchor = slot;
            if (lampObject != null) lamp = lampObject;
            if (grid.HasValue) gridSize = grid.Value;
            BuildContainer();
        }

        /// <summary>F2-Vertrag; das eigentliche Verhalten läuft über das Routing (SlotColumn → Player).</summary>
        public void Interact()
        {
        }

        /// <summary>Einlegen in Spalte (x,y): füllt den hintersten freien Slot. Nur passende Objekte;
        /// No-op bei voller Spalte, gesperrtem Fach oder leerer Hand.</summary>
        public void PlaceInColumn(int x, int y, PlayerCarry carry)
        {
            if (carry == null || _target == null || _target.IsClosed || carry.CarriedCount == 0 || !InRange(x, y))
            {
                return;
            }

            if (!carry.TryPeekTopComponent(out var top))
            {
                return;
            }

            var key = top.TryGetComponent<ISortable>(out var sortable) ? sortable.Key : default;
            // Jedes Item ist einlegbar; der Code (acceptedFacets) dient nur der Validierung:
            // SortTarget zählt korrekt/falsch und schließt nur ab, wenn genug korrekte und keine falschen drin sind.
            if (!TryGetFillCell(x, y, out var cx, out var cy, out var cz))
            {
                return; // Fach voll
            }

            if (!carry.TryHandOverTop(out var pickup) || pickup is not Component component)
            {
                return;
            }

            var id = component.GetInstanceID();
            if (!_target.TryPlace(id, key))
            {
                carry.TryPickup(pickup);
                return;
            }

            _grid[cx, cy, cz] = component;
            PlaceVisual(component, cx, cy, cz);

            if (_target.JustCompleted)
            {
                Close();
            }
        }

        /// <summary>Entnehmen aus Spalte (x,y): nimmt den vordersten belegten Slot in die Hand.
        /// No-op bei leerer Spalte, gesperrtem Fach oder Überlast.</summary>
        public void RemoveFromColumn(int x, int y, PlayerCarry carry)
        {
            if (carry == null || _target == null || _target.IsClosed || !InRange(x, y))
            {
                return;
            }

            if (!TryGetRemoveCell(x, y, out var cx, out var cy, out var cz))
            {
                return;
            }

            var component = _grid[cx, cy, cz];
            var weight = component.TryGetComponent<IPickup>(out var pickup) ? pickup.Weight : 0f;
            if (pickup == null || !carry.CanCarry(weight))
            {
                return; // Überlast oder nicht aufnehmbar
            }

            var id = component.GetInstanceID();
            if (!_target.TryRemove(id))
            {
                return;
            }

            _grid[cx, cy, cz] = null;
            RestoreVisual(component, id);
            carry.TryPickup(pickup); // setzt Physik auf „getragen"
        }

        private void Close()
        {
            if (lamp != null)
            {
                lamp.SetActive(true);
            }

            onCompleted?.Invoke();

            // Eingelegte Objekte bleiben sichtbar; das Fach wird gesperrt (Spalten-Collider aus).
            if (_columns != null)
            {
                foreach (var column in _columns)
                {
                    if (column != null) column.SetColliderEnabled(false);
                }
            }
        }
    }
}
