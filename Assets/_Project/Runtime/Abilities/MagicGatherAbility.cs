using System.Collections.Generic;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Items;
using UnityEngine;

namespace CozySanta.Runtime.Abilities
{
    /// <summary>
    /// Fähigkeit B „Heranholen": solange der Spieler ein Sortierobjekt hält, fliegen zufällige ruhende
    /// Kopien derselben Sorte (<see cref="PrefabId"/>) aus dem aktuellen Gebäude in die Hand. Eine
    /// Auslösung holt so viele, wie Ladungen UND freie Traglast erlauben – beide Faktoren begrenzen.
    /// </summary>
    public sealed class MagicGatherAbility : MagicAbility
    {
        private readonly List<Component> _candidates = new();

        public override void Activate()
        {
            if (!IsUnlocked || !Charges.HasCharge) return;
            if (!TryGetHeldTop(out var top, out _)) return;
            if (!top.TryGetComponent<PrefabId>(out var heldId) || string.IsNullOrEmpty(heldId.Key)) return;

            var area = CurrentArea();
            if (area == null) return;

            CollectRestingCopies(area, heldId.Key, top);
            Shuffle(_candidates);

            foreach (var item in _candidates)
            {
                if (!Charges.HasCharge) break;
                if (item == null || !item.TryGetComponent<IPickup>(out var pickup)) continue;
                if (!carry.CanCarry(pickup.Weight)) break; // Traglast erschöpft
                if (carry.TryPickup(pickup)) Charges.TryConsume();
            }
        }

        // Ruhende Items gleicher Sorte im Gebäude (unter ItemParent, Fallback: per Bereichs-AABB), ohne das gehaltene.
        private void CollectRestingCopies(ItemArea area, string key, Component held)
        {
            _candidates.Clear();
            var root = area.ItemParent;
            var ids = root != null
                ? root.GetComponentsInChildren<PrefabId>(includeInactive: false)
                : Object.FindObjectsByType<PrefabId>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var id in ids)
            {
                if (id == null || id.Key != key) continue;
                if (id.transform == held.transform) continue; // nicht das gehaltene Objekt
                if (!id.TryGetComponent<SettlingBody>(out var settling) || !settling.IsResting) continue;
                if (root == null && !area.Contains(id.transform.position)) continue;
                _candidates.Add(id);
            }
        }

        private static void Shuffle(IList<Component> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
