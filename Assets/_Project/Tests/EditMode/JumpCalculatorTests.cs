using CozySanta.Core.Player;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class JumpCalculatorTests
    {
        // J1: Absprunggeschwindigkeit erreicht die gewünschte Höhe (v0 = sqrt(2·g·h))
        [Test]
        public void ComputeJumpVelocity_ReachesRequestedHeight()
        {
            var v0 = JumpCalculator.ComputeJumpVelocity(1.1f, -9.81f);
            var apex = (v0 * v0) / (2f * 9.81f);
            Assert.AreEqual(1.1f, apex, 0.0001f);
        }

        // J2: Nicht-positive Höhe -> kein Absprung
        [Test]
        public void ComputeJumpVelocity_NonPositiveHeight_ReturnsZero()
        {
            Assert.AreEqual(0f, JumpCalculator.ComputeJumpVelocity(0f, -9.81f), 0.0001f);
            Assert.AreEqual(0f, JumpCalculator.ComputeJumpVelocity(-1f, -9.81f), 0.0001f);
        }

        // J3: Am Boden mit Sprunganforderung -> Vertikalgeschwindigkeit = Absprung
        [Test]
        public void StepVerticalVelocity_GroundedAndJump_ReturnsJumpVelocity()
        {
            var result = JumpCalculator.StepVerticalVelocity(
                current: -2f, grounded: true, jumpRequested: true,
                jumpVelocity: 5f, gravity: -9.81f, deltaTime: 0.016f);
            Assert.AreEqual(5f, result, 0.0001f);
        }

        // J4: Am Boden ohne Sprung -> sanfter Anpressdruck (-2) statt freiem Fall
        [Test]
        public void StepVerticalVelocity_GroundedNoJump_ClampsDownforce()
        {
            var result = JumpCalculator.StepVerticalVelocity(
                current: -50f, grounded: true, jumpRequested: false,
                jumpVelocity: 5f, gravity: -9.81f, deltaTime: 0.5f);
            // -2 (Anpressdruck) + gravity*dt
            Assert.AreEqual(-2f + (-9.81f * 0.5f), result, 0.0001f);
        }

        // J5: In der Luft -> Schwerkraft integriert, Sprunganforderung wirkungslos
        [Test]
        public void StepVerticalVelocity_Airborne_IntegratesGravityIgnoresJump()
        {
            var result = JumpCalculator.StepVerticalVelocity(
                current: 1f, grounded: false, jumpRequested: true,
                jumpVelocity: 5f, gravity: -9.81f, deltaTime: 0.1f);
            Assert.AreEqual(1f + (-9.81f * 0.1f), result, 0.0001f);
        }
    }
}
