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
    /// PlayMode-E2E-Tests für den Slot-Container (3D-Raster): Einlegen in eine Spalte (hinterster freier
    /// Slot), Vollständigkeit + Lampe + Schließen, Ablehnung nicht passender Objekte, Entnahme (vorderster
    /// belegter Slot) inkl. Kapazitätsprüfung. Treibt das Routing direkt über
    /// <see cref="SortTargetInteractable.PlaceInColumn"/> / <see cref="SortTargetInteractable.RemoveFromColumn"/>.
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

        private static SortTargetInteractable NewFach(string[] accepted, int required, GameObject lamp = null,
            Vector3Int? grid = null)
        {
            var go = new GameObject("fach");
            var fach = go.AddComponent<SortTargetInteractable>();
            fach.Configure(accepted, required, slot: null, lampObject: lamp, grid: grid ?? new Vector3Int(1, 1, 5));
            return fach;
        }

        // E1: passendes Objekt tragen + in Spalte (0,0) einlegen -> im Fach, Hand leer, korrekt gezählt
        [UnityTest]
        public IEnumerator Place_CorrectItem_IntoColumn()
        {
            var carry = NewCarry(10f);
            var fach = NewFach(new[] { "Europe", "Rot", "Stern" }, 3);
            var item = NewItem("Brief", 0.3f, "Europe", "Rot", "Stern");

            Assert.IsTrue(carry.TryPickup(item));
            fach.PlaceInColumn(0, 0, carry);

            Assert.AreEqual(0, carry.CarriedCount, "Hand ist nach dem Einsortieren leer.");
            Assert.AreEqual(fach.transform, item.transform.parent, "Brief liegt im Fach.");
            Assert.AreEqual(1, fach.Target.CorrectCount);

            yield return null;
            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
        }

        // E2: Spalte auf requiredCount füllen -> Lampe an, geschlossen; Einlagen bleiben sichtbar
        [UnityTest]
        public IEnumerator Complete_TurnsLampOn_AndCloses()
        {
            var carry = NewCarry(10f);
            var lamp = new GameObject("Lampe");
            lamp.SetActive(false);
            var fach = NewFach(new[] { "X" }, 2, lamp);

            var a = NewItem("A", 0.3f, "X");
            carry.TryPickup(a);
            fach.PlaceInColumn(0, 0, carry);

            var b = NewItem("B", 0.3f, "X");
            carry.TryPickup(b);
            fach.PlaceInColumn(0, 0, carry);

            Assert.IsTrue(fach.Target.IsClosed, "Fach ist abgeschlossen.");
            Assert.IsTrue(lamp.activeSelf, "Lampe leuchtet bei Vollständigkeit.");

            yield return null;
            Assert.IsTrue(a != null && b != null, "Eingelegte Objekte bleiben im Fach liegen.");

            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
            Object.Destroy(lamp);
        }

        // E3: nicht passendes Objekt -> wird abgelehnt (bleibt in der Hand), Lampe bleibt aus
        [UnityTest]
        public IEnumerator WrongItem_Rejected_StaysInHand()
        {
            var carry = NewCarry(10f);
            var lamp = new GameObject("Lampe");
            lamp.SetActive(false);
            var fach = NewFach(new[] { "Europe", "Rot", "Stern" }, 1, lamp);

            var wrong = NewItem("Falsch", 0.3f, "Asia", "Blau", "Teddy");
            carry.TryPickup(wrong);
            fach.PlaceInColumn(0, 0, carry);

            Assert.AreEqual(1, carry.CarriedCount, "Nicht passendes Objekt bleibt in der Hand.");
            Assert.AreEqual(0, fach.Target.Count, "Nichts wird ins Fach gelegt.");
            Assert.IsFalse(lamp.activeSelf, "Lampe bleibt aus.");

            yield return null;
            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
        }

        // E4: Entnahme nimmt den vordersten belegten Slot (zuletzt eingelegt liegt vorne -> kommt zuerst)
        [UnityTest]
        public IEnumerator Remove_TakesFrontmost()
        {
            var carry = NewCarry(10f);
            var fach = NewFach(new[] { "X" }, 5);

            var a = NewItem("A", 0.3f, "X");
            carry.TryPickup(a);
            fach.PlaceInColumn(0, 0, carry); // A -> hinten
            var b = NewItem("B", 0.3f, "X");
            carry.TryPickup(b);
            fach.PlaceInColumn(0, 0, carry); // B -> davor

            fach.RemoveFromColumn(0, 0, carry); // vorderster = B

            Assert.AreEqual(1, carry.CarriedCount);
            Assert.AreEqual(b, carry.TryPeekTopComponent(out var top) ? top : null, "B (vorne) wird zuerst entnommen.");
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
            fach.PlaceInColumn(0, 0, carry); // Box einsortiert

            carry.Capacity = 5f;             // Traglast jetzt zu klein für die Box
            fach.RemoveFromColumn(0, 0, carry);

            Assert.AreEqual(0, carry.CarriedCount, "Bei Überlast wird nicht entnommen.");
            Assert.AreEqual(1, fach.Target.Count, "Die Box bleibt im Fach.");

            yield return null;
            Object.Destroy(fach.gameObject);
            Object.Destroy(carry.gameObject);
        }
    }
}
