using System;
using System.Collections.Generic;
using CozySanta.Core.Keys;
using UnityEngine;

namespace CozySanta.Runtime.Keys
{
    /// <summary>
    /// Apply-Schicht des Schlüsselsystems. Liegt auf dem GameManager-Objekt.
    /// Hält <see cref="KeyInventory"/> und Icon-Zuordnung; benachrichtigt <see cref="KeyHudView"/>.
    /// </summary>
    public sealed class KeyInventoryManager : MonoBehaviour
    {
        [SerializeField] private KeyHudView hudView;

        private readonly KeyInventory _inventory = new KeyInventory();
        private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>();

        public event Action OnInventoryChanged;

        public KeyInventory Inventory => _inventory;

        public void CollectKey(string id, Sprite icon)
        {
            if (string.IsNullOrEmpty(id)) return;
            _inventory.AddKey(id);
            if (icon != null) _icons[id] = icon;
            NotifyHud();
        }

        public void ConsumeKeys(string[] ids)
        {
            _inventory.RemoveKeys(ids);
            foreach (var id in ids) _icons.Remove(id);
            NotifyHud();
        }

        public bool HasKeys(string[] ids) => _inventory.HasKeys(ids);

        private void NotifyHud()
        {
            OnInventoryChanged?.Invoke();
            if (hudView != null) hudView.Refresh(_inventory.GetAllKeys(), _icons);
        }
    }
}
