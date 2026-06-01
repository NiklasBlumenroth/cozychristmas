using System.Numerics;
using CozySanta.Core.Player;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class MovementCalculatorTests
    {
        // M1: Nulleingabe -> Nullvektor
        [Test]
        public void ComputeLocalVelocity_ZeroInput_ReturnsZero()
        {
            var result = MovementCalculator.ComputeLocalVelocity(Vector2.Zero, 5f);
            Assert.AreEqual(0f, result.Length(), 0.0001f);
        }

        // M2: Diagonale -> Betrag == speed (nicht schneller als geradeaus)
        [Test]
        public void ComputeLocalVelocity_Diagonal_MagnitudeEqualsSpeed()
        {
            var result = MovementCalculator.ComputeLocalVelocity(new Vector2(1f, 1f), 5f);
            Assert.AreEqual(5f, result.Length(), 0.0001f);
        }

        // M3: Geradeaus -> Betrag == speed
        [Test]
        public void ComputeLocalVelocity_Straight_MagnitudeEqualsSpeed()
        {
            var result = MovementCalculator.ComputeLocalVelocity(new Vector2(0f, 1f), 5f);
            Assert.AreEqual(5f, result.Length(), 0.0001f);
        }
    }
}
