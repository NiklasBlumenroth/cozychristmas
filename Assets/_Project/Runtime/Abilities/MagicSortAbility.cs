using System.Collections.Generic;
using CozySanta.Core.Sorting;
using CozySanta.Runtime.Sorting;
using UnityEngine;

namespace CozySanta.Runtime.Abilities
{
    /// <summary>
    /// Fähigkeit A „Auto-Einsortieren": schickt das oberste getragene Objekt vollautomatisch in ein
    /// passendes Ziel im aktuellen Gebäude – ein Regal-Fach (<see cref="SortTargetInteractable"/>) oder
    /// eine Truhe (<see cref="GiftChest"/>). Gibt es mehrere Ziele, wird bei jeder Auslösung zufällig
    /// eines gewählt. Eine Ladung = ein Objekt. Tut nichts (keine Ladung), wenn kein Ziel passt.
    /// </summary>
    public sealed class MagicSortAbility : MagicAbility
    {
        private readonly List<(SortTargetInteractable fach, int x, int y)> _shelves = new();
        private readonly List<GiftChest> _chests = new();

        public override void Activate()
        {
            if (!IsUnlocked || !Charges.HasCharge) return;
            if (!TryGetHeldTop(out _, out var key) || key.IsEmpty) return;

            var area = CurrentArea();
            if (area == null) return;

            CollectTargets(area, key);
            var total = _shelves.Count + _chests.Count;
            if (total == 0) return;

            var pick = Random.Range(0, total);
            if (pick < _shelves.Count)
            {
                var (fach, x, y) = _shelves[pick];
                var before = carry.CarriedCount;
                fach.PlaceInColumn(x, y, carry);
                if (carry.CarriedCount < before) Charges.TryConsume(); // nur bei tatsächlicher Einlage
            }
            else
            {
                var chest = _chests[pick - _shelves.Count];
                if (chest.TryAutoInsert(carry)) Charges.TryConsume();
            }
        }

        // Sammelt alle gültigen Ziele für diesen SortKey im aktuellen Gebäude.
        private void CollectTargets(Items.ItemArea area, SortKey key)
        {
            _shelves.Clear();
            _chests.Clear();

            var faecher = Object.FindObjectsByType<SortTargetInteractable>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var fach in faecher)
            {
                if (fach == null || !area.Contains(fach.transform.position)) continue;
                if (fach.TryFindFreeColumn(key, out var x, out var y)) _shelves.Add((fach, x, y));
            }

            var chests = Object.FindObjectsByType<GiftChest>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var chest in chests)
            {
                if (chest == null || !area.Contains(chest.transform.position)) continue;
                if (chest.CanAutoInsert(key)) _chests.Add(chest);
            }
        }
    }
}
