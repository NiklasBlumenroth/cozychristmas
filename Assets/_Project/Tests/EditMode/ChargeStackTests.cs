using CozySanta.Core.Abilities;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für die Ladungs-/Cooldown-Mechanik der magischen Sortierhilfen.</summary>
    public sealed class ChargeStackTests
    {
        // CS1: Startet voll.
        [Test]
        public void New_StartsFull()
        {
            var stack = new ChargeStack(3, 5f);
            Assert.AreEqual(3, stack.Current);
            Assert.IsTrue(stack.HasCharge);
            Assert.AreEqual(1f, stack.Fraction, 0.001f);
        }

        // CS2: Verbrauch zieht ab; bei 0 schlägt der Verbrauch fehl.
        [Test]
        public void Consume_DecrementsAndFailsAtZero()
        {
            var stack = new ChargeStack(2, 5f);
            Assert.IsTrue(stack.TryConsume());
            Assert.IsTrue(stack.TryConsume());
            Assert.AreEqual(0, stack.Current);
            Assert.IsFalse(stack.TryConsume());
        }

        // CS3: Nach Ablauf des Cooldowns kommt genau eine Ladung zurück.
        [Test]
        public void Step_AfterCooldown_RegainsOneCharge()
        {
            var stack = new ChargeStack(2, 5f);
            stack.TryConsume(); // 1
            stack.Step(2f);
            Assert.AreEqual(1, stack.Current, "vor Cooldown-Ende keine neue Ladung");
            stack.Step(3f);     // gesamt 5s
            Assert.AreEqual(2, stack.Current);
        }

        // CS4: Aufladen überschreitet das Maximum nicht.
        [Test]
        public void Step_DoesNotExceedMax()
        {
            var stack = new ChargeStack(2, 1f);
            stack.TryConsume();
            stack.Step(100f);
            Assert.AreEqual(2, stack.Current);
            Assert.AreEqual(1f, stack.Fraction, 0.001f);
        }

        // CS5: Configure klemmt Current; Refill füllt auf das Maximum.
        [Test]
        public void Configure_Clamps_And_Refill_TopsUp()
        {
            var stack = new ChargeStack(5, 4f);
            stack.Configure(2, 4f);          // Max sinkt -> Current geklemmt
            Assert.AreEqual(2, stack.Current);

            stack.TryConsume();
            stack.Configure(4, 4f);          // Max steigt -> Current bleibt (1)
            Assert.AreEqual(1, stack.Current);
            stack.Refill();
            Assert.AreEqual(4, stack.Current);
        }
    }
}
