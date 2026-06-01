using CozySanta.Core.Carry;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>
    /// EditMode-/Unit-Tests für die reine LIFO-/Gewichtslogik des Tragstapels.
    /// </summary>
    public sealed class CarryStackTests
    {
        // C1: Push unter Kapazität -> erfolgreich, Count/TotalWeight stimmen
        [Test]
        public void Push_UnderCapacity_Succeeds()
        {
            var stack = new CarryStack(1f);
            Assert.IsTrue(stack.TryPush(new CarryItem(1, 0.3f)));
            Assert.AreEqual(1, stack.Count);
            Assert.AreEqual(0.3f, stack.TotalWeight, 0.0001f);
        }

        // C2: Push genau an der Grenze (Summe == Capacity) -> erlaubt
        [Test]
        public void Push_AtCapacityBoundary_Allowed()
        {
            var stack = new CarryStack(1f);
            stack.TryPush(new CarryItem(1, 0.5f));
            Assert.IsTrue(stack.TryPush(new CarryItem(2, 0.5f)));
            Assert.AreEqual(2, stack.Count);
        }

        // C3: Push über Kapazität -> abgelehnt, Stapel unverändert
        [Test]
        public void Push_OverCapacity_Rejected()
        {
            var stack = new CarryStack(1f);
            stack.TryPush(new CarryItem(1, 0.8f));
            Assert.IsFalse(stack.TryPush(new CarryItem(2, 0.5f)));
            Assert.AreEqual(1, stack.Count);
            Assert.AreEqual(0.8f, stack.TotalWeight, 0.0001f);
        }

        // C4: LIFO -> Peek = zuletzt; Pop-Reihenfolge C,B,A
        [Test]
        public void Lifo_PeekAndPopOrder()
        {
            var stack = new CarryStack(100f);
            stack.TryPush(new CarryItem(1, 1f)); // A
            stack.TryPush(new CarryItem(2, 1f)); // B
            stack.TryPush(new CarryItem(3, 1f)); // C

            Assert.IsTrue(stack.TryPeek(out var top));
            Assert.AreEqual(3, top.Id);

            stack.TryPop(out var first);
            stack.TryPop(out var second);
            stack.TryPop(out var third);
            Assert.AreEqual(3, first.Id);
            Assert.AreEqual(2, second.Id);
            Assert.AreEqual(1, third.Id);
        }

        // C5: Pop auf leerem Stapel -> false, kein Fehler
        [Test]
        public void Pop_Empty_ReturnsFalse()
        {
            var stack = new CarryStack(1f);
            Assert.IsFalse(stack.TryPop(out _));
        }

        // C6: Capacity erhöhen -> vorher abgelehntes Objekt jetzt aufnehmbar
        [Test]
        public void IncreaseCapacity_PreviouslyRejected_NowFits()
        {
            var stack = new CarryStack(0.5f);
            Assert.IsFalse(stack.TryPush(new CarryItem(1, 1f)));
            stack.Capacity = 2f;
            Assert.IsTrue(stack.TryPush(new CarryItem(1, 1f)));
        }
    }
}
