using CozySanta.Core.Player;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class CrouchMotionTests
    {
        // CR1: Gehaltene Hocktaste fährt Richtung Hockhöhe, begrenzt durch speed·dt
        [Test]
        public void StepHeight_Held_MovesTowardCrouch_ClampedBySpeed()
        {
            var h = CrouchMotion.StepHeight(1.8f, 1.8f, 1.0f, crouchHeld: true, speed: 4f, deltaTime: 0.1f);
            Assert.AreEqual(1.8f - 0.4f, h, 0.0001f); // 4·0.1 = 0.4 Höhenänderung
        }

        // CR2: Losgelassen fährt Richtung Stehhöhe
        [Test]
        public void StepHeight_Released_MovesTowardStand()
        {
            var h = CrouchMotion.StepHeight(1.0f, 1.8f, 1.0f, crouchHeld: false, speed: 4f, deltaTime: 0.1f);
            Assert.AreEqual(1.0f + 0.4f, h, 0.0001f);
        }

        // CR3: Erreicht das Ziel ohne Überschwingen
        [Test]
        public void StepHeight_ReachesTargetWithoutOvershoot()
        {
            var h = CrouchMotion.StepHeight(1.05f, 1.8f, 1.0f, crouchHeld: true, speed: 100f, deltaTime: 1f);
            Assert.AreEqual(1.0f, h, 0.0001f);
        }

        // CR4: Mittelpunkt hält die Füße fix (Unterkante = center - height/2 bleibt gleich)
        [Test]
        public void CenterY_KeepsFeetFixed()
        {
            const float standH = 1.8f, standCenter = 0.9f, crouchH = 1.0f;
            var bottomStand = standCenter - (standH * 0.5f);
            var cy = CrouchMotion.CenterY(standCenter, standH, crouchH);
            var bottomCrouch = cy - (crouchH * 0.5f);
            Assert.AreEqual(bottomStand, bottomCrouch, 0.00001f);
        }

        // CR5: Augenhöhe proportional, mit Fallback bei nicht-positiver Stehhöhe
        [Test]
        public void EyeHeight_ProportionalAndFallback()
        {
            Assert.AreEqual(0.8f, CrouchMotion.EyeHeight(0.9f, 1.8f, 1.6f), 0.0001f);
            Assert.AreEqual(1.6f, CrouchMotion.EyeHeight(1.8f, 1.8f, 1.6f), 0.0001f);
            Assert.AreEqual(1.6f, CrouchMotion.EyeHeight(1.0f, 0f, 1.6f), 0.0001f); // Fallback
        }
    }
}
