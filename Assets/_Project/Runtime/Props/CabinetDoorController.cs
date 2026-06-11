using CozySanta.Core.Props;
using CozySanta.Runtime.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Props
{
    /// <summary>
    /// Einzeln per Blick + E schaltbare Schranktür (Apply-Schicht). Dreht den Scharnier-Pivot
    /// (Standard: dieses Transform) zwischen „zu" und „offen"; die reine Auf/Zu-Logik liegt in
    /// <see cref="DoorMotion"/> (Core). Voraussetzung: der Objekt-Ursprung liegt auf der
    /// Scharnierkante (in Blender setzen) und das Türobjekt trägt einen nicht-Trigger-Collider,
    /// damit der Fadenkreuz-Raycast es trifft.
    /// </summary>
    public sealed class CabinetDoorController : MonoBehaviour, IInteractable
    {
        [Header("Scharnier")]
        [Tooltip("Drehpunkt/Tür. Leer = dieses Transform (Ursprung muss am Scharnier liegen).")]
        [SerializeField] private Transform doorPivot;
        [Tooltip("Öffnungswinkel in Grad (Vorzeichen umkehren, falls die Tür falsch herum aufgeht).")]
        [SerializeField] private float openAngle = 100f;
        [Tooltip("Lokale Scharnierachse (meist Y = nach oben).")]
        [SerializeField] private Vector3 hingeAxis = Vector3.up;
        [Tooltip("Dauer für Auf-/Zubewegung in Sekunden.")]
        [SerializeField] private float openDuration = 0.5f;

        [Header("Hinweis")]
        [SerializeField] private string openPrompt = "Öffnen";
        [SerializeField] private string closePrompt = "Schließen";

        private DoorMotion _motion;
        private Transform _pivot;
        private Quaternion _closedRotation;

        public string PromptText => _motion != null && _motion.TargetsOpen ? closePrompt : openPrompt;

        private void Awake()
        {
            _pivot = doorPivot != null ? doorPivot : transform;
            _closedRotation = _pivot.localRotation;
            _motion = new DoorMotion(openDuration);
        }

        /// <summary>Blick + E: Tür auf/zu schalten (kehrt auch mitten in der Bewegung um).</summary>
        public void Interact() => _motion?.Toggle();

        private void Update()
        {
            if (_motion == null || !_motion.IsMoving)
            {
                return;
            }

            _motion.Step(UnityEngine.Time.deltaTime);
            var open = _closedRotation * Quaternion.AngleAxis(openAngle, hingeAxis.normalized);
            _pivot.localRotation = Quaternion.Slerp(_closedRotation, open, _motion.Progress01);
        }
    }
}
