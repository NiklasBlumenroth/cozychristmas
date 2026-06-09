using System.Collections.Generic;
using CozySanta.Core.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Runtime-Implementierung von <see cref="IInteractionProbe"/> über Unity-Physics. Zielt
    /// fadenkreuz-genau: ein <see cref="Physics.Raycast"/> von der Kamera (<see cref="view"/>) entlang
    /// der Blickrichtung durch die Bildschirmmitte. Getroffen wird genau das, worauf das Fadenkreuz
    /// zeigt; der getroffene Collider wird über <c>GetComponentInParent</c> auf ein
    /// <see cref="IInteractable"/> aufgelöst (Distanz = Trefferabstand, Winkel = 0). Die Auswahlregel
    /// (Reichweite/Winkel) bleibt in <see cref="CozySanta.Core.Interaction.InteractionSelector"/>.
    /// </summary>
    public sealed class PhysicsInteractionProbe : MonoBehaviour, IInteractionProbe, IInteractableResolver
    {
        [SerializeField] private Transform view;
        [Tooltip("Maximale Strahllänge (Reichweite) des Fadenkreuz-Raycasts in Metern.")]
        [SerializeField] private float radius = 3f;
        [SerializeField] private LayerMask mask = ~0;

        private readonly List<InteractionCandidate> _buffer = new List<InteractionCandidate>();
        private readonly Dictionary<int, IInteractable> _map = new Dictionary<int, IInteractable>();

        public IReadOnlyList<InteractionCandidate> QueryCandidates()
        {
            _buffer.Clear();
            _map.Clear();
            var origin = view != null ? view : transform;

            // Fadenkreuz-Strahl durch die Bildschirmmitte (Kamera-Position + Blickrichtung).
            if (!Physics.Raycast(origin.position, origin.forward, out var hit, radius, mask, QueryTriggerInteraction.Ignore))
            {
                return _buffer;
            }

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable is not Component component)
            {
                return _buffer;
            }

            var id = component.GetInstanceID();
            // Winkel = 0: der Treffer liegt per Definition auf dem Strahl. Distanz = echter Trefferabstand.
            _buffer.Add(new InteractionCandidate(id, hit.distance, 0f));
            _map[id] = interactable;
            return _buffer;
        }

        public bool TryResolve(int targetId, out IInteractable interactable)
        {
            return _map.TryGetValue(targetId, out interactable);
        }
    }
}
