using CozySanta.Core.Interaction;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Sorting;
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
        [SerializeField] private PlayerCarry carry;

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

        /// <summary>Vom Interact-Input aufzurufen. Löst nur bei aufgelöstem Fokus aus (Gate in Core).
        /// Ist das fokussierte Objekt aufnehmbar (<see cref="IPickup"/>), wird es an <c>PlayerCarry</c>
        /// geroutet; sonst die generische <c>Interact()</c>-Aktion ausgelöst.</summary>
        public void TryInteract()
        {
            if (!InteractionTrigger.ShouldInteract(HasInteractableFocus, true) || _focused == null)
            {
                return;
            }

            if (carry != null && _focused is IPickup pickup)
            {
                carry.TryPickup(pickup);
                return;
            }

            // F4: fokussiertes Sortier-Fach erhält die Interaktion mit Trag-Kontext (Einsortieren/Entnehmen).
            if (carry != null && _focused is SortTargetInteractable sortTarget)
            {
                sortTarget.HandleInteract(carry);
                return;
            }

            _focused.Interact();
        }

        /// <summary>Nehmen (Linksklick): fokussiertes Aufnehmbares in die Hand, oder ein Objekt aus dem
        /// fokussierten Fach entnehmen. Ohne Fokus passiert nichts.</summary>
        public void TryTake()
        {
            if (_focused == null || carry == null)
            {
                return;
            }

            if (_focused is IPickup pickup)
            {
                carry.TryPickup(pickup);
                return;
            }

            if (_focused is SortTargetInteractable sortTarget)
            {
                sortTarget.RemoveToHand(carry);
            }
        }

        /// <summary>Ablegen (Rechtsklick): in das fokussierte Fach einsortieren; sonst das oberste
        /// getragene Objekt vor dem Spieler fallen lassen.</summary>
        public void TryPlace()
        {
            if (carry == null)
            {
                return;
            }

            if (_focused is SortTargetInteractable sortTarget)
            {
                sortTarget.PlaceFromHand(carry);
                return;
            }

            carry.Drop();
        }
    }
}
