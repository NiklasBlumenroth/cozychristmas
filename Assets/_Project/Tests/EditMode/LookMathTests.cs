using CozySanta.Core.Player;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class LookMathTests
    {
        // L1: innerhalb des Bereichs -> einfach addiert
        [Test]
        public void ClampPitch_WithinRange_AddsDelta()
        {
            var result = LookMath.ClampPitch(10f, 5f, -80f, 80f);
            Assert.AreEqual(15f, result, 0.0001f);
        }

        // L2: über max -> auf max begrenzt
        [Test]
        public void ClampPitch_AboveMax_ClampedToMax()
        {
            var result = LookMath.ClampPitch(75f, 20f, -80f, 80f);
            Assert.AreEqual(80f, result, 0.0001f);
        }

        // L3: unter min -> auf min begrenzt
        [Test]
        public void ClampPitch_BelowMin_ClampedToMin()
        {
            var result = LookMath.ClampPitch(-75f, -20f, -80f, 80f);
            Assert.AreEqual(-80f, result, 0.0001f);
        }
    }
}
