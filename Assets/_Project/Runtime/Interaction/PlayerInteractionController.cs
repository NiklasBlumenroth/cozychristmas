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

        [Header("Ghost-Vorschau (optional)")]
        [Tooltip("Optionaler Material-Override für die Einlage-Vorschau. Bleibt das Feld leer, wird der " +
                 "Klon zur Laufzeit aus dem Originalmaterial abgeleitet und grün-transparent eingefärbt.")]
        [SerializeField] private Material ghostMaterial;

        private IInteractionProbe _probe;
        private IInteractableResolver _resolver;
        private IInteractable _focused;
        private SortGhostPreview _ghostPreview;

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

            _ghostPreview = new SortGhostPreview(ghostMaterial);
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

            UpdateGhostPreview();
        }

        /// <summary>Zeigt eine durchscheinende Einlage-Vorschau, wenn ein offenes, nicht volles Fach
        /// anvisiert wird und der Spieler etwas trägt; sonst wird die Vorschau versteckt.</summary>
        private void UpdateGhostPreview()
        {
            if (_ghostPreview == null)
            {
                return;
            }

            var fach = _focused as SortTargetInteractable;
            Component top = null;
            if (carry != null && carry.CarriedCount > 0)
            {
                carry.TryPeekTopComponent(out top);
            }

            _ghostPreview.Set(fach, top);
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

            Debug.Log($"[SortDbg] TryTake (Linksklick): Fokus={(_focused == null ? "<keiner>" : _focused.GetType().Name)}, carry={carry.CarriedCount}.");

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

        /// <summary>Einsortieren (Rechtsklick): NUR in ein fokussiertes Fach. Ohne Fach-Fokus passiert
        /// bewusst nichts mehr (Ablegen läuft jetzt über <see cref="TryDrop"/> / Taste Q), damit Briefe
        /// nicht versehentlich neben dem Fach fallen gelassen werden.</summary>
        public void TryPlace()
        {
            if (carry == null)
            {
                return;
            }

            Debug.Log($"[SortDbg] TryPlace (Rechtsklick): Fokus={(_focused == null ? "<keiner>" : _focused.GetType().Name)}, carry={carry.CarriedCount}.");

            if (_focused is SortTargetInteractable sortTarget)
            {
                sortTarget.PlaceFromHand(carry);
                return;
            }

            Debug.Log("[SortDbg] TryPlace: kein Fach fokussiert -> kein Einsortieren (Ablegen jetzt via Q).");
        }

        /// <summary>Ablegen (Taste Q): lässt das oberste getragene Objekt vor dem Spieler fallen.</summary>
        public void TryDrop()
        {
            if (carry == null)
            {
                return;
            }

            carry.Drop();
        }
    }
}
