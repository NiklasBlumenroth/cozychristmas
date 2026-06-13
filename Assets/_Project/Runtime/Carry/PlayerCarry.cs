using System.Collections.Generic;
using CozySanta.Core.Carry;
using UnityEngine;

namespace CozySanta.Runtime.Carry
{
    /// <summary>
    /// Hält den Core-Tragstapel und wendet die Seiteneffekte an (Apply): Reparenting an die
    /// Hand-Anker, Physik ruhen lassen, Weltplatzierung beim Ablegen. Reihenfolge/Gewicht kommen
    /// ausschließlich aus <see cref="CarryStack"/>.
    /// </summary>
    public sealed class PlayerCarry : MonoBehaviour
    {
        [SerializeField] private float capacity = 5f;
        [SerializeField] private Transform leftHandAnchor;   // Kind der Kamera: aktuelles Objekt, screen-fix
        [SerializeField] private Transform rightHandAnchor;  // Kind des Spielerkörpers: Stapel, feste Höhe
        [SerializeField] private float stackSpacing = 0.35f;
        [SerializeField] private float dropDistance = 1.2f;

        private CarryStack _stack;
        private readonly Dictionary<int, IPickup> _objects = new Dictionary<int, IPickup>();

        /// <summary>Maximale Traglast (kg). Setzbar für spätere Tragkraft-Upgrades (F6).</summary>
        public float Capacity
        {
            get => _stack != null ? _stack.Capacity : capacity;
            set
            {
                capacity = value;
                if (_stack != null)
                {
                    _stack.Capacity = value;
                }
            }
        }

        /// <summary>Anzahl aktuell getragener Objekte.</summary>
        public int CarriedCount => _stack != null ? _stack.Count : 0;

        /// <summary>True, wenn ein Objekt mit <paramref name="weight"/> noch in die Traglast passt.</summary>
        public bool CanCarry(float weight) => _stack != null && _stack.CanPickUp(weight);

        /// <summary>Liest das oberste getragene Objekt als <see cref="Component"/> (LIFO) ohne Entnahme –
        /// nur lesend, z. B. als Quelle für die Ghost-Vorschau. False bei leerem Stapel.</summary>
        public bool TryPeekTopComponent(out Component component)
        {
            component = null;
            if (_stack == null || !_stack.TryPeek(out var item) || !_objects.TryGetValue(item.Id, out var pickup))
            {
                return false;
            }

            component = pickup as Component;
            return component != null;
        }

        /// <summary>
        /// Übergibt das oberste getragene Objekt an einen Aufrufer (z. B. ein Fach beim Einsortieren):
        /// entnimmt es aus dem Tragstapel, OHNE es in der Welt abzulegen oder seine Physik zu verändern –
        /// der Aufrufer übernimmt Reparenting/Physik. False bei leerem Stapel.
        /// </summary>
        public bool TryHandOverTop(out IPickup pickup)
        {
            pickup = null;
            if (_stack == null || !_stack.TryPeek(out var item) || !_objects.TryGetValue(item.Id, out pickup))
            {
                return false;
            }

            _stack.TryPop(out _);
            _objects.Remove(item.Id);
            RelayoutHands();
            return true;
        }

        private void Awake()
        {
            _stack = new CarryStack(capacity);
        }

        /// <summary>Konfiguriert die Hand-Anker (z. B. aus Setup-Code oder Tests).</summary>
        public void SetAnchors(Transform left, Transform right)
        {
            leftHandAnchor = left;
            rightHandAnchor = right;
        }

        /// <summary>Versucht, ein aufnehmbares Objekt aufzunehmen. False bei Überlast.</summary>
        public bool TryPickup(IPickup pickup)
        {
            if (pickup is not Component component)
            {
                return false;
            }

            var id = component.GetInstanceID();
            // Schutz gegen Doppel-Aufnahme desselben Objekts (z. B. wenn der Interact-Input zweimal
            // feuert): sonst entstehen doppelte Stapel-Einträge mit gleicher Id, _stack und _objects
            // laufen auseinander → Lücken im Tragestapel, „2 auf einmal", hängender letzter Brief.
            if (_objects.ContainsKey(id))
            {
                return false;
            }

            var item = new CarryItem(id, pickup.Weight);
            if (!_stack.TryPush(item))
            {
                return false;
            }

            _objects[item.Id] = pickup;
            SetCarriedPhysics(component, carried: true);
            RelayoutHands();
            return true;
        }

        /// <summary>Legt das zuletzt aufgenommene Objekt (LIFO) vor dem Spieler ab.</summary>
        public void Drop()
        {
            if (!_stack.TryPop(out var item))
            {
                return;
            }

            if (_objects.TryGetValue(item.Id, out var pickup) && pickup is Component component)
            {
                _objects.Remove(item.Id);
                component.transform.SetParent(null, worldPositionStays: true);
                var origin = leftHandAnchor != null ? leftHandAnchor : transform;
                component.transform.SetPositionAndRotation(
                    origin.position + (origin.forward * dropDistance), Quaternion.identity);
                SetCarriedPhysics(component, carried: false);

                // Abgelegtes Item wieder in die Simulation wecken, damit es fällt und danach erneut
                // einschläft (ruhend = kinematisch, aber aufhebbar). Nur relevant für Items mit Ruhe-Controller.
                if (component.TryGetComponent<CozySanta.Runtime.Items.SettlingBody>(out var settling))
                {
                    settling.BeginSettling();
                }
            }

            RelayoutHands();
        }

        /// <summary>Ordnet getragene Objekte neu an: oberstes nach links, übrige nach oben gestapelt rechts.</summary>
        private void RelayoutHands()
        {
            var items = _stack.Items;
            for (var i = 0; i < items.Count; i++)
            {
                if (!_objects.TryGetValue(items[i].Id, out var pickup) || pickup is not Component component)
                {
                    continue;
                }

                var isTop = i == items.Count - 1;
                if (isTop && leftHandAnchor != null)
                {
                    component.transform.SetParent(leftHandAnchor, worldPositionStays: false);
                    component.transform.localPosition = Vector3.zero;
                    component.transform.localRotation = Quaternion.identity;
                }
                else if (rightHandAnchor != null)
                {
                    component.transform.SetParent(rightHandAnchor, worldPositionStays: false);
                    component.transform.localPosition = Vector3.up * (stackSpacing * i);
                    component.transform.localRotation = Quaternion.identity;
                }
            }
        }

        private static void SetCarriedPhysics(Component component, bool carried)
        {
            // Alle Collider im Objekt (auch an Kind-Objekten, z. B. der Brief-Collider am Kind „Cube")
            // deaktivieren, sonst trifft der Fadenkreuz-Raycast getragene Objekte vor der Kamera.
            foreach (var collider in component.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                collider.enabled = !carried;
            }

            if (component.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = carried;
                // Schwerkraft beim Tragen aus, beim Ablegen wieder an. Wichtig, weil im Fach abgelegte
                // Objekte useGravity=false bekommen (MarkPlaced) – sonst schweben sie nach dem Ablegen.
                body.useGravity = !carried;
            }
        }
    }
}
