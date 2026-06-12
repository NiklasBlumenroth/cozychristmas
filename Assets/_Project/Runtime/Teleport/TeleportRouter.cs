using System;
using System.Collections.Generic;
using CozySanta.Core.Teleport;
using CozySanta.Runtime.Areas;
using CozySanta.Runtime.Player;
using UnityEngine;

namespace CozySanta.Runtime.Teleport
{
    /// <summary>
    /// Zentrale Teleport-Verdrahtung (Apply): hält eine Liste aus Paaren „Trigger-Collider → Ziel".
    /// Berührt der Spieler einen Trigger, wird er zur Position des zugehörigen Ziel-Transforms versetzt
    /// (optional auch dessen Blickrichtung). Der Re-Entry-Schutz läuft über den Core-<see cref="TeleportArbiter"/>.
    ///
    /// Die Collider müssen Szenen-Objekte sein (daher Komponente statt ScriptableObject). Trigger-Events
    /// feuern in Unity nur am Collider-Objekt selbst – der Router hängt dafür beim Start an jeden Trigger
    /// automatisch einen <see cref="TeleportTriggerForwarder"/>, der Betreten/Verlassen hierher meldet.
    /// </summary>
    public sealed class TeleportRouter : MonoBehaviour
    {
        [Serializable]
        public sealed class Pair
        {
            [Tooltip("Trigger-Collider: berührt der Spieler diesen, wird teleportiert (isTrigger wird erzwungen).")]
            public Collider trigger;
            [Tooltip("Ziel: leeres GameObject. Der Spieler wird auf dessen Position gesetzt.")]
            public Transform destination;
            [Tooltip("Wenn an: der Spieler übernimmt zusätzlich die Blickrichtung (Y-Drehung) des Ziels.")]
            public bool faceDestination = true;
            [Tooltip("Optional: Bereichs-Root, der bei diesem Teleport aktiviert wird (alle anderen aus). " +
                     "Leer = keine Bereichs-Umschaltung.")]
            public GameObject activateArea;
        }

        [Tooltip("Liste der Teleport-Paare: pro Zeile ein Trigger-Collider und sein Ziel.")]
        [SerializeField] private List<Pair> pairs = new List<Pair>();
        [Tooltip("Optional: schaltet beim Teleport den Ziel-Bereich aktiv (siehe 'Activate Area' je Paar).")]
        [SerializeField] private AreaActivator areaActivator;

        private readonly TeleportArbiter _arbiter = new TeleportArbiter();

        private void Awake()
        {
            for (var i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair?.trigger == null)
                {
                    Debug.LogWarning($"[Teleport] Paar {i} hat keinen Trigger-Collider – übersprungen.", this);
                    continue;
                }
                if (pair.destination == null)
                {
                    Debug.LogWarning($"[Teleport] Paar {i} ('{pair.trigger.name}') hat kein Ziel – übersprungen.", this);
                    continue;
                }

                pair.trigger.isTrigger = true;
                var forwarder = pair.trigger.gameObject.AddComponent<TeleportTriggerForwarder>();
                forwarder.Bind(this, i);
            }
        }

        /// <summary>Vom Forwarder gemeldetes Betreten von Trigger <paramref name="index"/>.</summary>
        public void HandleEnter(int index, Collider other)
        {
            var player = other.GetComponentInParent<FirstPersonController>();
            if (player == null) return;
            if (!_arbiter.ShouldTeleport(index)) return;

            Teleport(player, pairs[index]);
        }

        /// <summary>Vom Forwarder gemeldetes Verlassen von Trigger <paramref name="index"/>.</summary>
        public void HandleExit(int index, Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            _arbiter.NotifyExit(index);
        }

        private void Teleport(FirstPersonController player, Pair pair)
        {
            // Zielbereich VOR dem Versetzen aktivieren, damit Boden/Collider beim Landen schon live sind.
            if (areaActivator != null && pair.activateArea != null)
            {
                areaActivator.Activate(pair.activateArea);
            }

            var dest = pair.destination;
            var controller = player.GetComponent<CharacterController>();

            // CharacterController widersetzt sich direkter Positionssetzung → kurz deaktivieren.
            if (controller != null) controller.enabled = false;

            player.transform.position = dest.position;
            if (pair.faceDestination)
            {
                player.transform.rotation = Quaternion.Euler(0f, dest.eulerAngles.y, 0f);
            }
            player.ResetVerticalVelocity();

            if (controller != null) controller.enabled = true;

            // Belegung neu aufbauen: erst alles freigeben (der Quell-Trigger feuert beim Wegteleportieren
            // KEIN OnTriggerExit, würde sonst dauerhaft belegt bleiben), dann exakt die am Ziel
            // überlappenden Trigger als belegt markieren – so bleibt der Bounce-Schutz, ohne Hängenbleiben.
            _arbiter.Reset();
            var playerBounds = controller != null ? controller.bounds : default;
            for (var i = 0; i < pairs.Count; i++)
            {
                var t = pairs[i]?.trigger;
                if (t == null) continue;
                if (controller != null ? t.bounds.Intersects(playerBounds) : t.bounds.Contains(dest.position))
                {
                    _arbiter.MarkOccupied(i);
                }
            }
        }
    }
}
