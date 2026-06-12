using CozySanta.Core.Areas;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests der exklusiven Bereichs-Aktivierung (<see cref="AreaActivation"/>).</summary>
    public sealed class AreaActivationTests
    {
        // AC1 – genau ein Bereich aktiv
        [Test]
        public void AC1_ExactlyOneActive()
        {
            CollectionAssert.AreEqual(new[] { false, true, false }, AreaActivation.Resolve(3, 1));
        }

        // AC2 – negativer Index: alle inaktiv (kein Komplett-Abschalten erzwingen)
        [Test]
        public void AC2_NegativeIndex_AllInactive()
        {
            CollectionAssert.AreEqual(new[] { false, false, false }, AreaActivation.Resolve(3, -1));
        }

        // AC3 – Index außerhalb der Grenzen: alle inaktiv
        [Test]
        public void AC3_OutOfRange_AllInactive()
        {
            CollectionAssert.AreEqual(new[] { false, false, false }, AreaActivation.Resolve(3, 5));
        }

        // AC4 – count 0 liefert leeres Array
        [Test]
        public void AC4_ZeroCount_Empty()
        {
            Assert.AreEqual(0, AreaActivation.Resolve(0, 0).Length);
        }
    }
}
