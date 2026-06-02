using System.Collections;
using CozySanta.Core.Sorting;
using CozySanta.Runtime.Carry;
using CozySanta.Runtime.Sorting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CozySanta.Tests.PlayMode
{
    /// <summary>
    /// PlayMode-E2E-Tests für F4 (E1–E4): Einsortieren, Vollständigkeit+Lampe+Schließen, sanftes
    /// Fehlerfeedback und Entnahme inkl. Kapazitätsprüfung. Treibt das Routing direkt über
    /// <see cref="SortTargetInteractable.HandleInteract"/>.
    /// </summary>
    public sealed class SortingPlayModeTests
    {
        private sealed class FakeItem : MonoBehaviour, IPickup, ISortable
        {
            public float WeightValue = 0.3f;
            public SortKey KeyValue;
            public float Weight => WeightValue;
            public SortKey Key => KeyValue;
        }

        private static FakeItem NewItem(string name, float weight, params string[] facets)
        {
            var go = new GameObject(name);
            var item = go.AddComponent<FakeItem>();
            item.WeightValue = weight;
            item.KeyValue = new SortKey(facets);
            return item;
        }

        private static PlayerCarry NewCarry(float capacity)
        {
            var player = new GameObject("player");
            var carry = player.AddComponent<PlayerCarry>();
            carry.Capacity = capacity;
            carry.SetAnchors(new GameObject("left").transform, new GameObject("right").transform);
            return carry;
        }

        private static SortTargetInteractable NewFach(string[] accepted, int required, GameObject lamp = null)
        {
            var go = new GameObject("fach");
            var fach = go.AddComponent<SortTargetInteractable>();
            fach.Configure(accepted, required, slot: null, lampObject: lamp);
            return fach;
        }

        // E1: passendes Objekt tragen + Fach-Interact -> im Fach, Hand leer, korrekt gezählt
        [UnityTest]
        public IEnumerator Place_CorrectItem_IntoFach()
        {
            var carry = NewCarry(10f);
            var fach = NewFach(new[] { "Europe", "Rot", "Stern" }, 3);
            var item = NewItem("Brief", 0.3f, "Europe", "Rot", "Stern");

            Assert.IsTrue(carry.TryPickup(item));
            fach.HandleInteract(carry);

            Assert.AreEqual(0, carry.CarriedCount, "Hand ist nach dem Einsortieren leer.");
            Assert.AreEqual(fach.transform, item.transform.parent, "Brief liegt im Fach.");
            Assert.AreEqual(1, fach.Target.CorrectCount);

            yield return null;
            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
        }

        // E2: Fach auf requiredCount korrekte füllen -> Lampe an, geschlossen, Visuals entfernt
        [UnityTest]
        public IEnumerator Complete_TurnsLampOn_ClosesAndRemovesVisuals()
        {
            var carry = NewCarry(10f);
            var lamp = new GameObject("Lampe");
            lamp.SetActive(false);
            var fach = NewFach(new[] { "X" }, 2, lamp);

            var a = NewItem("A", 0.3f, "X");
            carry.TryPickup(a);
            fach.HandleInteract(carry);

            var b = NewItem("B", 0.3f, "X");
            carry.TryPickup(b);
            fach.HandleInteract(carry);

            Assert.IsTrue(fach.Target.IsClosed, "Fach ist abgeschlossen.");
            Assert.IsTrue(lamp.activeSelf, "Lampe leuchtet bei Vollständigkeit.");

            yield return null; // Destroy der Einlagen greift im nächsten Frame
            Assert.IsTrue(a == null, "Eingelegte Visuals werden beim Schließen entfernt.");
            Assert.IsTrue(b == null);

            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
            Object.Destroy(lamp);
        }

        // E3: falsches Objekt -> bleibt im Fach, Lampe bleibt aus, nicht abgeschlossen
        [UnityTest]
        public IEnumerator WrongItem_StaysAndLampStaysOff()
        {
            var carry = NewCarry(10f);
            var lamp = new GameObject("Lampe");
            lamp.SetActive(false);
            var fach = NewFach(new[] { "Europe", "Rot", "Stern" }, 1, lamp);

            var wrong = NewItem("Falsch", 0.3f, "Asia", "Blau", "Teddy");
            carry.TryPickup(wrong);
            fach.HandleInteract(carry);

            Assert.AreEqual(1, fach.Target.WrongCount);
            Assert.AreEqual(SortTargetState.FalschEnthalten, fach.Target.Evaluate());
            Assert.IsFalse(fach.Target.IsClosed);
            Assert.IsFalse(lamp.activeSelf, "Lampe bleibt bei falscher Sortierung aus.");
            Assert.AreEqual(fach.transform, wrong.transform.parent, "Falsches Objekt bleibt liegen.");

            yield return null;
            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
            Object.Destroy(lamp);
        }

        // E4: Entnahme bei leerer Hand -> zuletzt eingelegtes (LIFO) kehrt zurück
        [UnityTest]
        public IEnumerator RemoveTop_ReturnsToHand_LIFO()
        {
            var carry = NewCarry(10f);
            var fach = NewFach(new[] { "X" }, 5);

            var a = NewItem("A", 0.3f, "X");
            carry.TryPickup(a);
            fach.HandleInteract(carry); // A einsortiert
            var b = NewItem("B", 0.3f, "X");
            carry.TryPickup(b);
            fach.HandleInteract(carry); // B einsortiert (Hand danach leer)

            fach.HandleInteract(carry); // Hand leer -> Entnahme (LIFO -> B)

            Assert.AreEqual(1, carry.CarriedCount);
            Assert.AreEqual(1, fach.Target.Count, "Nach der Entnahme ist nur noch ein Objekt im Fach.");

            yield return null;
            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
        }

        // E4b: Entnahme bei Überlast -> abgelehnt, Objekt bleibt im Fach
        [UnityTest]
        public IEnumerator Remove_Overload_Rejected()
        {
            var carry = NewCarry(10f);
            var fach = NewFach(new[] { "X" }, 5);

            var heavy = NewItem("Box", 8f, "X");
            carry.TryPickup(heavy);
            fach.HandleInteract(carry); // Box einsortiert

            carry.Capacity = 5f;        // Traglast jetzt zu klein für die Box
            fach.HandleInteract(carry); // Hand leer -> Entnahme versucht

            Assert.AreEqual(0, carry.CarriedCount, "Bei Überlast wird nicht entnommen.");
            Assert.AreEqual(1, fach.Target.Count, "Die Box bleibt im Fach.");

            yield return null;
            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
        }
    }
}
