using CozySanta.Core.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Orchestriert den Beispiel-Slice: holt Kandidaten über den Provider (Probe),
    /// ruft die reine Entscheidung <see cref="InteractionSelector.Decide"/> (Core) und wendet
    /// das Ergebnis an (Apply). Die Auswahlregel liegt ausschließlich in der Core-Schicht;
    /// hier findet nur der Seiteneffekt statt (gesetztes Fokusziel).
    /// </summary>
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private float maxRange = 3f;
        [SerializeField] private float maxAngle = 30f;

        private IInteractionProbe _probe;

        /// <summary>True, wenn aktuell ein Ziel fokussiert ist.</summary>
        public bool HasFocus { get; private set; }

        /// <summary>Kennung des fokussierten Ziels (gültig, wenn <see cref="HasFocus"/>).</summary>
        public int FocusedTargetId { get; private set; }

        /// <summary>Injiziert die Weltabfrage (Runtime-Probe oder Test-Fake).</summary>
        public void Configure(IInteractionProbe probe)
        {
            _probe = probe;
        }

        private void Awake()
        {
            if (_probe == null)
            {
                _probe = GetComponent<IInteractionProbe>();
            }
        }

        private void Update()
        {
            if (_probe != null)
            {
                Tick();
            }
        }

        /// <summary>Ein Durchlauf Probe -> Decide -> Apply. Öffentlich für deterministische Tests.</summary>
        public void Tick()
        {
            var candidates = _probe.QueryCandidates();
            var selection = InteractionSelector.Decide(candidates, new SelectionSettings(maxRange, maxAngle));

            HasFocus = selection.HasTarget;
            FocusedTargetId = selection.HasTarget ? selection.TargetId : 0;
        }
    }
}
