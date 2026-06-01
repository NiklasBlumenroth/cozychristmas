using System.Collections.Generic;
using CozySanta.Core.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Runtime-Implementierung von <see cref="IInteractionProbe"/> über Unity-Physics.
    /// Liefert ausschließlich Kandidaten, die ein <see cref="IInteractable"/> tragen, samt Distanz
    /// und Blickwinkel, und löst die gewählte TargetId pro Frame wieder auf (<see cref="IInteractableResolver"/>).
    /// </summary>
    public sealed class PhysicsInteractionProbe : MonoBehaviour, IInteractionProbe, IInteractableResolver
    {
        [SerializeField] private Transform view;
        [SerializeField] private float radius = 3f;
        [SerializeField] private LayerMask mask = ~0;

        private readonly List<InteractionCandidate> _buffer = new List<InteractionCandidate>();
        private readonly Dictionary<int, IInteractable> _map = new Dictionary<int, IInteractable>();
        private readonly Collider[] _hits = new Collider[32];

        public IReadOnlyList<InteractionCandidate> QueryCandidates()
        {
            _buffer.Clear();
            _map.Clear();
            var origin = view != null ? view : transform;

            var count = Physics.OverlapSphereNonAlloc(origin.position, radius, _hits, mask);
            for (var i = 0; i < count; i++)
            {
                var interactable = _hits[i].GetComponentInParent<IInteractable>();
                if (interactable is not Component component)
                {
                    continue;
                }

                var id = component.GetInstanceID();
                if (_map.ContainsKey(id))
                {
                    continue;
                }

                var to = component.transform.position - origin.position;
                var distance = to.magnitude;
                var angle = Vector3.Angle(origin.forward, to);
                _buffer.Add(new InteractionCandidate(id, distance, angle));
                _map[id] = interactable;
            }

            return _buffer;
        }

        public bool TryResolve(int targetId, out IInteractable interactable)
        {
            return _map.TryGetValue(targetId, out interactable);
        }
    }
}
