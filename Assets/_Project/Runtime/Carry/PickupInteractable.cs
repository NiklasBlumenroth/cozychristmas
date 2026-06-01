using CozySanta.Runtime.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Carry
{
    /// <summary>
    /// Aufnehmbares Weltobjekt. Ist sowohl <see cref="IInteractable"/> (Fokus/Hinweis aus F2) als auch
    /// <see cref="IPickup"/>. Das eigentliche Aufnehmen wird vom <c>PlayerInteractionController</c>
    /// an den <c>PlayerCarry</c> geroutet; <see cref="Interact"/> bleibt daher absichtlich leer.
    /// </summary>
    public sealed class PickupInteractable : MonoBehaviour, IInteractable, IPickup
    {
        [SerializeField] private string promptText = "Aufnehmen";
        [SerializeField] private float weight = 0.3f;

        public string PromptText => promptText;

        public float Weight => weight;

        public void Interact()
        {
            // Aufnehmen erfolgt spielerseitig über PlayerCarry (Routing im PlayerInteractionController).
        }
    }
}
