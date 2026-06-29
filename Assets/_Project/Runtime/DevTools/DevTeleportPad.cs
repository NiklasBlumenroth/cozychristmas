using System;
using System.Collections.Generic;
using CozySanta.Runtime.Areas;
using CozySanta.Runtime.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.DevTools
{
    /// <summary>
    /// Entwickler-/Debug-Hilfe (kein Gameplay-UI im Sinne der Constitution V): direktes Springen zu
    /// festen Teleport-Zielen über den Ziffernblock (Numpad 7/8/9/0/6 = Slot 1–5). Auf den Numpad verlegt,
    /// weil die oberen Zifferntasten 2/3 nun die magischen Sortierhilfen auslösen. Wird dem Helfer-Objekt im
    /// Teleporter zugewiesen; jeder Slot bekommt ein Ziel-Transform (z. B. Position in Sektor 1, Sektor 2 …).
    ///
    /// Versetzt wird – wie im <see cref="Teleport.TeleportRouter"/> – CharacterController-sicher
    /// (kurz deaktivieren) inkl. optionaler Blickrichtung und optionaler Bereichs-Umschaltung
    /// (damit beim Sprung in einen anderen Sektor dessen Root aktiv ist).
    /// </summary>
    public sealed class DevTeleportPad : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            [Tooltip("Frei wählbarer Name (nur zur Orientierung im Inspector, z. B. 'Sektor 1').")]
            public string label;
            [Tooltip("Ziel: leeres GameObject. Der Spieler wird auf dessen Position gesetzt.")]
            public Transform destination;
            [Tooltip("Wenn an: der Spieler übernimmt zusätzlich die Blickrichtung (Y-Drehung) des Ziels.")]
            public bool faceDestination = true;
            [Tooltip("Optional: Bereichs-Root, der bei diesem Sprung aktiviert wird (alle anderen aus). " +
                     "Leer = keine Bereichs-Umschaltung. Benötigt einen gesetzten Area Activator.")]
            public GameObject activateArea;
        }

        [Tooltip("Ziele für Numpad 7/8/9/0/6 (Reihenfolge = Slot 1–5). Maximal 5 werden gelesen.")]
        [SerializeField] private List<Slot> slots = new List<Slot>(5);
        [Tooltip("Optional: schaltet beim Sprung den Ziel-Bereich aktiv (siehe 'Activate Area' je Slot). " +
                 "Leer = wird beim Start automatisch gesucht.")]
        [SerializeField] private AreaActivator areaActivator;
        [Tooltip("Der zu versetzende Spieler. Leer = wird beim Start automatisch gesucht.")]
        [SerializeField] private FirstPersonController player;

        // Numpad 7/8/9/0/6 (Index 0 = Slot 1). Reihenfolge passt zu den Slots.
        private Key[] _keys;

        private void Awake()
        {
            if (player == null) player = FindAnyObjectByType<FirstPersonController>();
            if (areaActivator == null) areaActivator = FindAnyObjectByType<AreaActivator>();

            _keys = new[] { Key.Numpad7, Key.Numpad8, Key.Numpad9, Key.Numpad0, Key.Numpad6 };
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            for (var i = 0; i < _keys.Length; i++)
            {
                if (keyboard[_keys[i]].wasPressedThisFrame)
                {
                    TeleportTo(i);
                    break;
                }
            }
        }

        /// <summary>Versetzt den Spieler zum Slot <paramref name="index"/> (0 = Taste „1").</summary>
        public void TeleportTo(int index)
        {
            if (index < 0 || index >= slots.Count) return;

            var slot = slots[index];
            if (slot?.destination == null)
            {
                Debug.LogWarning($"[DevTeleport] Slot {index + 1} hat kein Ziel – übersprungen.", this);
                return;
            }

            if (player == null) player = FindAnyObjectByType<FirstPersonController>();
            if (player == null)
            {
                Debug.LogWarning("[DevTeleport] Kein FirstPersonController gefunden.", this);
                return;
            }

            // Zielbereich VOR dem Versetzen aktivieren, damit Boden/Collider beim Landen schon live sind.
            if (areaActivator != null && slot.activateArea != null)
            {
                areaActivator.Activate(slot.activateArea);
            }

            var controller = player.GetComponent<CharacterController>();

            // CharacterController widersetzt sich direkter Positionssetzung → kurz deaktivieren.
            if (controller != null) controller.enabled = false;

            player.transform.position = slot.destination.position;
            if (slot.faceDestination)
            {
                player.transform.rotation = Quaternion.Euler(0f, slot.destination.eulerAngles.y, 0f);
            }
            player.ResetVerticalVelocity();

            if (controller != null) controller.enabled = true;
        }
    }
}
