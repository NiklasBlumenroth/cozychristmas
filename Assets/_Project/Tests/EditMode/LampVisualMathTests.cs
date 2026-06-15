using CozySanta.Core.Snow;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class LampVisualMathTests
    {
        // LV1: Pulse bleibt in [0,1] und liegt bei sin=0 in der Mitte (0.5).
        [Test]
        public void Pulse_StaysInRange_AndCentersAtHalf()
        {
            Assert.AreEqual(0.5f, LampVisualMath.Pulse(0f, 1f), 0.0001f);
            for (var i = 0; i < 50; i++)
            {
                var v = LampVisualMath.Pulse(i * 0.137f, 0.7f);
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
            }
        }

        // LV2: Warmth klemmt an den Enden (0 / 1) und überschreitet sie auch bei Über-/Unterlauf nicht.
        [Test]
        public void Warmth_ClampsToEndpoints()
        {
            Assert.AreEqual(0f, LampVisualMath.Warmth(0f), 0.0001f);
            Assert.AreEqual(1f, LampVisualMath.Warmth(1f), 0.0001f);
            Assert.AreEqual(0f, LampVisualMath.Warmth(-3f), 0.0001f);
            Assert.AreEqual(1f, LampVisualMath.Warmth(5f), 0.0001f);
        }

        // LV3: Warmth ist mittensymmetrisch (Smoothstep) -> 0.5 bei halbem Akku.
        [Test]
        public void Warmth_MidpointIsHalf()
        {
            Assert.AreEqual(0.5f, LampVisualMath.Warmth(0.5f), 0.0001f);
        }

        // LV4: TargetLevel skaliert mit Akku und schaltet zwischen idle/aktiv.
        [Test]
        public void TargetLevel_ScalesWithBatteryAndState()
        {
            Assert.AreEqual(6f, LampVisualMath.TargetLevel(1f, active: true, idleLevel: 2f, activeLevel: 6f), 0.0001f);
            Assert.AreEqual(2f, LampVisualMath.TargetLevel(1f, active: false, idleLevel: 2f, activeLevel: 6f), 0.0001f);
            Assert.AreEqual(0f, LampVisualMath.TargetLevel(0f, active: true, idleLevel: 2f, activeLevel: 6f), 0.0001f);
            Assert.AreEqual(1f, LampVisualMath.TargetLevel(0.5f, active: false, idleLevel: 2f, activeLevel: 6f), 0.0001f);
        }

        // LV5: SmoothTowards lässt bei dt=0/speed=0 unverändert, nähert sich sonst dem Ziel ohne Überschwingen.
        [Test]
        public void SmoothTowards_NoStepOnZero_AndConvergesWithoutOvershoot()
        {
            Assert.AreEqual(3f, LampVisualMath.SmoothTowards(3f, 10f, speed: 0f, deltaTime: 0.1f), 0.0001f);
            Assert.AreEqual(3f, LampVisualMath.SmoothTowards(3f, 10f, speed: 5f, deltaTime: 0f), 0.0001f);

            var v = 0f;
            for (var i = 0; i < 200; i++)
            {
                v = LampVisualMath.SmoothTowards(v, 10f, speed: 6f, deltaTime: 0.016f);
                Assert.LessOrEqual(v, 10f);
            }
            Assert.AreEqual(10f, v, 0.05f);
        }
    }
}
