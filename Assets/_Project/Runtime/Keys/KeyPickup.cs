using CozySanta.Runtime.Interaction;
using UnityEngine;

namespace CozySanta.Runtime.Keys
{
    /// <summary>
    /// Physisches Schlüssel-Objekt in der Welt. Implementiert <see cref="IInteractable"/>; geht
    /// NICHT in den Carry-Stack, sondern landet im <see cref="KeyInventoryManager"/>.
    /// </summary>
    public sealed class KeyPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string keyId = "";
        [SerializeField] private Sprite keyIcon;
        [SerializeField] private KeyInventoryManager inventoryManager;

        public string PromptText => "Schlüssel aufnehmen";

        private void Awake()
        {
            // Fallback-ID damit ein Schlüssel ohne Inspector-Eintrag trotzdem funktioniert.
            if (string.IsNullOrEmpty(keyId))
                keyId = $"key_{GetInstanceID()}";
        }

        private void Start()
        {
            if (inventoryManager == null)
                inventoryManager = Object.FindFirstObjectByType<KeyInventoryManager>();

            if (inventoryManager == null)
                Debug.LogWarning("[KeyPickup] Kein KeyInventoryManager in der Szene gefunden. Setup F8 ausführen.", this);
        }

        public void Interact()
        {
            if (inventoryManager == null)
                inventoryManager = Object.FindFirstObjectByType<KeyInventoryManager>();

            if (inventoryManager == null)
            {
                Debug.LogWarning("[KeyPickup] Kein KeyInventoryManager – Schlüssel kann nicht aufgenommen werden.", this);
                return;
            }

            inventoryManager.CollectKey(keyId, keyIcon);
            Destroy(gameObject);
        }
    }
}
