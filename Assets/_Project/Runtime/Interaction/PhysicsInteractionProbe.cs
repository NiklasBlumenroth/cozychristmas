using System.Collections.Generic;
using CozySanta.Core.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Runtime-Implementierung von <see cref="IInteractionProbe"/> über Unity-Physics.
    /// Liefert Kandidaten im Umkreis des Blickursprungs samt Distanz und Blickwinkel.
    /// Bewusst minimal – die vollständige Interaktion entsteht in F2.
    /// </summary>
    public sealed class PhysicsInteractionProbe : MonoBehaviour, IInteractionProbe
    {
        [SerializeField] private Transform view;
        [SerializeField] private float radius = 3f;
        [SerializeField] private LayerMask mask = ~0;

        private readonly List<InteractionCandidate> _buffer = new List<InteractionCandidate>();
        private readonly Collider[] _hits = new Collider[16];

        public IReadOnlyList<InteractionCandidate> QueryCandidates()
        {
            _buffer.Clear();
            var origin = view != null ? view : transform;

            var count = Physics.OverlapSphereNonAlloc(origin.position, radius, _hits, mask);
            for (var i = 0; i < count; i++)
            {
                var t = _hits[i].transform;
                var to = t.position - origin.position;
                var distance = to.magnitude;
                var angle = Vector3.Angle(origin.forward, to);
                _buffer.Add(new InteractionCandidate(t.GetInstanceID(), distance, angle));
            }

            return _buffer;
        }
    }
}
