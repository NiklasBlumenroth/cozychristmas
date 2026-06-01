using CozySanta.Runtime.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozySanta.Runtime.Player
{
    /// <summary>
    /// Input-Adapter: bindet die Input-System-Actions der Map „Player" an die entkoppelten Setter
    /// des Controllers. Auf dasselbe GameObject wie eine <c>PlayerInput</c>-Komponente (Behavior
    /// „Send Messages") setzen; die Action-Namen Move/Look/Interact werden per Konvention
    /// (OnMove/OnLook/OnInteract) aufgerufen.
    /// </summary>
    public sealed class PlayerInputRelay : MonoBehaviour
    {
        [SerializeField] private FirstPersonController controller;
        [SerializeField] private PlayerInteractionController interaction;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<FirstPersonController>();
            }

            if (interaction == null)
            {
                interaction = GetComponent<PlayerInteractionController>();
            }
        }

        public void OnMove(InputValue value)
        {
            if (controller != null)
            {
                controller.SetMoveInput(value.Get<Vector2>());
            }
        }

        public void OnLook(InputValue value)
        {
            if (controller != null)
            {
                controller.SetLookInput(value.Get<Vector2>());
            }
        }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed && interaction != null)
            {
                interaction.TryInteract();
            }
        }
    }
}
