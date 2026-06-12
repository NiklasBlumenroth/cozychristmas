using System.Collections.Generic;
using UnityEngine;

namespace CozySanta.Runtime.Areas
{
    /// <summary>
    /// Koordiniert die betretenen <see cref="AreaZone"/>s und das (geteilte) HUD-Panel.
    /// Es besitzt immer genau eine Zone das HUD: die <b>zuletzt betretene</b> noch aktive Zone.
    /// Deren <see cref="AreaZone.HudSection"/> wird eingeblendet und deren <see cref="AreaZone.Tracker"/>
    /// auf „aktiv" gesetzt (nur er beschreibt das HUD). Beim Verlassen übernimmt die nächst-jüngste
    /// noch aktive Zone; gibt es keine, wird das Panel ausgeblendet. So gibt es keine Konflikte,
    /// selbst wenn sich Zonen räumlich überlappen.
    /// </summary>
    public sealed class AreaManager : MonoBehaviour
    {
        // Reihenfolge = Eintrittsreihenfolge; das letzte Element ist die aktuelle HUD-Zone.
        private readonly List<AreaZone> _activeZones = new List<AreaZone>();

        public IReadOnlyList<AreaZone> ActiveZones => _activeZones;

        public void RegisterZone(AreaZone zone)
        {
            if (zone == null || _activeZones.Contains(zone)) return;
            _activeZones.Add(zone);
            UpdateHud();
        }

        public void UnregisterZone(AreaZone zone)
        {
            if (zone == null || !_activeZones.Remove(zone)) return;
            // Verlassene Zone in jedem Fall abschalten (sie ist nicht mehr in der Liste).
            Deactivate(zone);
            UpdateHud();
        }

        private void UpdateHud()
        {
            var current = _activeZones.Count > 0 ? _activeZones[_activeZones.Count - 1] : null;

            // Erst alle nicht-aktuellen aktiven Zonen abschalten …
            foreach (var zone in _activeZones)
            {
                if (zone != current) Deactivate(zone);
            }

            // … dann die aktuelle einschalten (zuletzt, falls Panels geteilt werden).
            if (current != null)
            {
                if (current.HudSection != null) current.HudSection.SetActive(true);
                if (current.Tracker != null) current.Tracker.SetHudActive(true);
            }
        }

        private static void Deactivate(AreaZone zone)
        {
            if (zone.Tracker != null) zone.Tracker.SetHudActive(false);
            if (zone.HudSection != null) zone.HudSection.SetActive(false);
        }
    }
}
