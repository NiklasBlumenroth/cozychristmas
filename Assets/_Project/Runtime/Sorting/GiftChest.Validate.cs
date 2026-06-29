using System.Collections.Generic;
using CozySanta.Core.Sorting;
using CozySanta.Runtime.Items;
using CozySanta.Runtime.Rendering;
using UnityEngine;

namespace CozySanta.Runtime.Sorting
{
    /// <summary>
    /// Apply-Teil der Truhe: sammelt beim Schließen den Inhalt aus dem Innenvolumen, lässt
    /// <see cref="GiftChestValidation"/> entscheiden und führt die Seiteneffekte aus
    /// (annehmen = zerstören + zählen, abweisen = alles zur Röhre auswerfen).
    /// </summary>
    public sealed partial class GiftChest
    {
        // Wiederverwendeter Puffer für Physics.OverlapBox (kein Per-Klick-GC).
        private static readonly Collider[] s_overlap = new Collider[128];

        /// <summary>Sammelt den Inhalt, validiert ihn und schließt anschließend den Deckel. Verriegelt,
        /// wenn die Annahme die Soll-Menge erreicht.</summary>
        private void ValidateAndClose()
        {
            var items = CollectContained();
            if (items.Count == 0)
            {
                CloseLid(thenLock: false);
                return;
            }

            var keys = new List<SortKey>(items.Count);
            foreach (var it in items) keys.Add(it.Key);

            var decision = GiftChestValidation.Decide(keys, Accepted, _acceptedCount, required);

            if (decision.Reject)
            {
                foreach (var it in items) Eject(it);
                CloseLid(thenLock: false);
                return;
            }

            foreach (var it in items) Accept(it);
            _acceptedCount += decision.AcceptCount;
            if (decision.AcceptCount > 0) onItemsAccepted?.Invoke(decision.AcceptCount);

            CloseLid(thenLock: decision.Locks);
        }

        /// <summary>Sammelt eindeutige <see cref="Sortable"/>-Items im Innenvolumen.</summary>
        private List<Sortable> CollectContained()
        {
            var result = new List<Sortable>();
            if (insideVolume == null) return result;

            var t = insideVolume.transform;
            var center = t.TransformPoint(insideVolume.center);
            var halfExtents = Vector3.Scale(insideVolume.size * 0.5f, AbsScale(t.lossyScale));
            var count = Physics.OverlapBoxNonAlloc(center, halfExtents, s_overlap, t.rotation, itemMask,
                                                   QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var col = s_overlap[i];
                if (col == null) continue;
                var sortable = col.GetComponentInParent<Sortable>();
                if (sortable != null && !result.Contains(sortable)) result.Add(sortable);
            }

            return result;
        }

        // Annehmen: aus dem Instanz-Draw abmelden (falls registriert) und zerstören – das Item verschwindet.
        private static void Accept(Sortable item)
        {
            UnregisterFromInstancing(item.transform);
            Object.Destroy(item.gameObject);
        }

        // Abweisen: zur Röhre versetzen (mit Streuung) und wieder fallen lassen, sodass es zurück in den
        // Raum rollt. Bleibt aufhebbar; meldet sich beim erneuten Ruhen selbst wieder zum Instanz-Draw an.
        private void Eject(Sortable item)
        {
            UnregisterFromInstancing(item.transform);

            var origin = ejectTarget != null ? ejectTarget : transform;
            var scatter = Random.insideUnitCircle * ejectScatter;
            var pos = origin.position + new Vector3(scatter.x, 0f, scatter.y);
            item.transform.SetPositionAndRotation(pos, Random.rotationUniform);

            if (item.TryGetComponent<SettlingBody>(out var settling))
            {
                settling.BeginSettling();
            }
            else if (item.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = false;
                body.useGravity = true;
            }
        }

        private static void UnregisterFromInstancing(Transform itemRoot)
        {
            itemRoot.GetComponentInParent<InstancedItemRenderer>(includeInactive: true)?.Unregister(itemRoot);
        }

        private static Vector3 AbsScale(Vector3 s) => new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
    }
}
