using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CozySanta.Runtime.Keys
{
    /// <summary>
    /// Zeigt gehaltene Schlüssel als Icons am Bildschirmrand (editor-authored Slots).
    /// Laufzeitcode setzt nur Sprite und active-Flag; kein UI wird erzeugt.
    /// </summary>
    public sealed class KeyHudView : MonoBehaviour
    {
        [SerializeField] private Image[] iconSlots = new Image[0];

        /// <summary>Aufgerufen von <see cref="KeyInventoryManager"/> nach jeder Änderung.</summary>
        public void Refresh(IReadOnlyCollection<string> heldKeys, IReadOnlyDictionary<string, Sprite> icons)
        {
            var keyList = new List<string>(heldKeys);
            for (var i = 0; i < iconSlots.Length; i++)
            {
                if (iconSlots[i] == null) continue;
                if (i < keyList.Count)
                {
                    iconSlots[i].gameObject.SetActive(true);
                    if (icons.TryGetValue(keyList[i], out var sprite))
                        iconSlots[i].sprite = sprite;
                }
                else
                {
                    iconSlots[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
