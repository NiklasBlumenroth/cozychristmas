using System.Collections.Generic;
using UnityEngine;

namespace CozySanta.Runtime.Areas
{
    /// <summary>
    /// Koordiniert aktive <see cref="AreaZone"/>s: zeigt/versteckt deren HUD-Sektionen.
    /// Liegt auf dem GameManager-Objekt. Kein Stack, keine Priorität – alle aktiven Zones
    /// sind gleichzeitig sichtbar; bei Überlapp werden beide Aufgabenlisten angezeigt.
    /// </summary>
    public sealed class AreaManager : MonoBehaviour
    {
        private readonly HashSet<AreaZone> _activeZones = new HashSet<AreaZone>();

        public IReadOnlyCollection<AreaZone> ActiveZones => _activeZones;

        public void RegisterZone(AreaZone zone)
        {
            if (zone == null || !_activeZones.Add(zone)) return;
            if (zone.HudSection != null) zone.HudSection.SetActive(true);
        }

        public void UnregisterZone(AreaZone zone)
        {
            if (zone == null || !_activeZones.Remove(zone)) return;
            if (zone.HudSection != null) zone.HudSection.SetActive(false);
        }
    }
}
