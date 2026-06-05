using System.Collections.Generic;

namespace CozySanta.Core.Keys
{
    /// <summary>
    /// Menge der aktuell gehaltenen Schlüssel-IDs. Entscheidungslogik ohne Unity-Abhängigkeiten.
    /// </summary>
    public sealed class KeyInventory
    {
        private readonly HashSet<string> _keys = new HashSet<string>();

        public void AddKey(string id)
        {
            if (!string.IsNullOrEmpty(id)) _keys.Add(id);
        }

        public void RemoveKeys(IEnumerable<string> ids)
        {
            foreach (var id in ids) _keys.Remove(id);
        }

        /// <summary>True wenn alle übergebenen IDs im Inventar vorhanden sind.</summary>
        public bool HasKeys(IEnumerable<string> ids)
        {
            foreach (var id in ids)
                if (!_keys.Contains(id)) return false;
            return true;
        }

        public IReadOnlyCollection<string> GetAllKeys() => _keys;
    }
}
