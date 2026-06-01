using System.Collections;
using CozySanta.Runtime.Carry;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CozySanta.Tests.PlayMode
{
    /// <summary>
    /// PlayMode-E2E-Tests für Aufnehmen, Stapeln (Links/Rechts), Ablegen (LIFO) und Überlast.
    /// </summary>
    public sealed class CarryPlayModeTests
    {
        private sealed class FakePickup : MonoBehaviour, IPickup
        {
            public float WeightValue = 0.3f;
            public float Weight => WeightValue;
        }

        private static FakePickup NewPickup(string name, float weight)
        {
            var go = new GameObject(name);
            var pickup = go.AddComponent<FakePickup>();
            pickup.WeightValue = weight;
            return pickup;
        }

        private static (PlayerCarry carry, Transform left, Transform right) NewPlayer(float capacity)
        {
            var player = new GameObject("player");
            var carry = player.AddComponent<PlayerCarry>();
            carry.Capacity = capacity;
            var left = new GameObject("left").transform;
            var right = new GameObject("right").transform;
            carry.SetAnchors(left, right);
            return (carry, left, right);
        }

        // E1: Aufnehmen -> Objekt in linker Hand, gezählt
        [UnityTest]
        public IEnumerator Pickup_GoesToLeftHand()
        {
            var (carry, left, right) = NewPlayer(10f);
            var a = NewPickup("A", 0.3f);

            Assert.IsTrue(carry.TryPickup(a));
            Assert.AreEqual(1, carry.CarriedCount);
            Assert.AreEqual(left, a.transform.parent);

            yield return null;
            Object.Destroy(carry.gameObject);
            Object.Destroy(left.gameObject);
            Object.Destroy(right.gameObject);
            Object.Destroy(a.gameObject);
        }

        // E2: Stapeln -> zuletzt aufgenommenes links, vorheriges in den rechten Stapel
        [UnityTest]
        public IEnumerator Stacking_LatestLeft_RestRight()
        {
            var (carry, left, right) = NewPlayer(10f);
            var a = NewPickup("A", 0.3f);
            var b = NewPickup("B", 0.3f);

            carry.TryPickup(a);
            carry.TryPickup(b);

            Assert.AreEqual(2, carry.CarriedCount);
            Assert.AreEqual(left, b.transform.parent, "Zuletzt aufgenommenes (B) gehört nach links.");
            Assert.AreEqual(right, a.transform.parent, "Vorheriges (A) gehört in den rechten Stapel.");

            yield return null;
            Object.Destroy(carry.gameObject);
            Object.Destroy(left.gameObject);
            Object.Destroy(right.gameObject);
            Object.Destroy(a.gameObject);
            Object.Destroy(b.gameObject);
        }

        // E3: Ablegen (LIFO) -> oberstes zurück in die Welt, vorheriges rückt nach links
        [UnityTest]
        public IEnumerator Drop_ReturnsTopToWorld_LIFO()
        {
            var (carry, left, right) = NewPlayer(10f);
            var a = NewPickup("A", 0.3f);
            var b = NewPickup("B", 0.3f);
            carry.TryPickup(a);
            carry.TryPickup(b);

            carry.Drop();

            Assert.AreEqual(1, carry.CarriedCount);
            Assert.IsNull(b.transform.parent, "Zuletzt aufgenommenes (B) wird zuerst abgelegt (wieder in der Welt).");
            Assert.AreEqual(left, a.transform.parent, "A rückt nach dem Ablegen nach links.");

            yield return null;
            Object.Destroy(carry.gameObject);
            Object.Destroy(left.gameObject);
            Object.Destroy(right.gameObject);
            Object.Destroy(a.gameObject);
            Object.Destroy(b.gameObject);
        }

        // E4: Überlast -> Aufnahme abgelehnt, Objekt bleibt in der Welt
        [UnityTest]
        public IEnumerator Overweight_NotPickedUp()
        {
            var (carry, left, right) = NewPlayer(0.5f);
            var heavy = NewPickup("Heavy", 1.0f);

            Assert.IsFalse(carry.TryPickup(heavy));
            Assert.AreEqual(0, carry.CarriedCount);
            Assert.AreNotEqual(left, heavy.transform.parent);

            yield return null;
            Object.Destroy(carry.gameObject);
            Object.Destroy(left.gameObject);
            Object.Destroy(right.gameObject);
            Object.Destroy(heavy.gameObject);
        }
    }
}
