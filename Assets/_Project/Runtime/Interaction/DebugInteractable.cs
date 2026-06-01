using UnityEngine;

namespace CozySanta.Runtime.Interaction
{
    /// <summary>
    /// Einfaches Test-/Debug-Interactable für manuelle Spieltests: wechselt bei <see cref="Interact"/>
    /// sichtbar die Farbe und loggt. Auf ein Objekt mit Renderer + Collider setzen.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class DebugInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptText = "Interagieren";
        [SerializeField] private Color interactedColor = Color.green;

        private Renderer _renderer;
        private bool _toggled;

        public string PromptText => promptText;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        public void Interact()
        {
            _toggled = !_toggled;
            if (_renderer != null)
            {
                _renderer.material.color = _toggled ? interactedColor : Color.white;
            }

            Debug.Log($"[DebugInteractable] Interact: {name}");
        }
    }
}
