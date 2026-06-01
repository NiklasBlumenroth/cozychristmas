using CozySanta.Core.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Orchestriert die Interaktion: Kandidaten über den Probe holen, Fokus über die reine
    /// F1-Entscheidung <see cref="InteractionSelector.Decide"/> bestimmen, das gewählte Ziel auf ein
    /// <see cref="IInteractable"/> auflösen, den Hinweis steuern und bei Tastendruck <c>Interact</c>
    /// auslösen. Auswahl- und Gate-Regeln liegen in der Core-Schicht; hier nur die Seiteneffekte.
    /// </summary>
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private float maxRange = 3f;
        [SerializeField] private float maxAngle = 30f;
        [SerializeField] private InteractionPromptPresenter prompt;

        private IInteractionProbe _probe;
        private IInteractableResolver _resolver;
        private IInteractable _focused;

        /// <summary>Rohes Fokusergebnis der F1-Entscheidung.</summary>
        public bool HasFocus { get; private set; }

        /// <summary>Kennung des gewählten Ziels (F1).</summary>
        public int FocusedTargetId { get; private set; }

        /// <summary>True, wenn der Fokus auf ein konkretes <see cref="IInteractable"/> aufgelöst wurde.</summary>
        public bool HasInteractableFocus => _focused != null;

        /// <summary>Aktuell fokussiertes interagierbares Objekt (oder null).</summary>
        public IInteractable FocusedInteractable => _focused;

        public void Configure(IInteractionProbe probe) => Configure(probe, null, null);

        public void Configure(IInteractionProbe probe, IInteractableResolver resolver)
            => Configure(probe, resolver, null);

        public void Configure(IInteractionProbe probe, IInteractableResolver resolver, InteractionPromptPresenter promptPresenter)
        {
            _probe = probe;
            _resolver = resolver;
            if (promptPresenter != null)
            {
                prompt = promptPresenter;
            }
        }

        private void Awake()
        {
            if (_probe == null)
            {
                _probe = GetComponent<IInteractionProbe>();
            }

            if (_resolver == null)
            {
                _resolver = GetComponent<IInteractableResolver>();
            }
        }

        private void Update()
        {
            if (_probe != null)
            {
                Tick();
            }
        }

        /// <summary>Ein Durchlauf: Probe -> Decide -> Auflösen -> Hinweis. Öffentlich für Tests.</summary>
        public void Tick()
        {
            var candidates = _probe.QueryCandidates();
            var selection = InteractionSelector.Decide(candidates, new SelectionSettings(maxRange, maxAngle));

            HasFocus = selection.HasTarget;
            FocusedTargetId = selection.HasTarget ? selection.TargetId : 0;

            _focused = null;
            if (selection.HasTarget && _resolver != null
                && _resolver.TryResolve(selection.TargetId, out var interactable))
            {
                _focused = interactable;
            }

            if (prompt != null)
            {
                if (_focused != null)
                {
                    prompt.Show(_focused.PromptText);
                }
                else
                {
                    prompt.Hide();
                }
            }
        }

        /// <summary>Vom Interact-Input aufzurufen. Löst nur bei aufgelöstem Fokus aus (Gate in Core).</summary>
        public void TryInteract()
        {
            if (InteractionTrigger.ShouldInteract(HasInteractableFocus, true) && _focused != null)
            {
                _focused.Interact();
            }
        }
    }
}
