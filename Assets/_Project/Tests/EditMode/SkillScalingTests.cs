using CozySanta.Core.Progression;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für die Stufen-Mathematik (steigend, fallend, ganzzahlig, Randfälle).</summary>
    public sealed class SkillScalingTests
    {
        // SC1: Steigender Wert – Start, Mitte, Ende.
        [Test]
        public void Value_Increasing_StartMidEnd()
        {
            Assert.AreEqual(5f,  SkillScaling.Value(5f, 25f, 0, 20),  0.001f);
            Assert.AreEqual(15f, SkillScaling.Value(5f, 25f, 10, 20), 0.001f);
            Assert.AreEqual(25f, SkillScaling.Value(5f, 25f, 20, 20), 0.001f);
        }

        // SC2: Fallender Wert (Cooldown) interpoliert korrekt nach unten.
        [Test]
        public void Value_Decreasing_Works()
        {
            Assert.AreEqual(8f, SkillScaling.Value(8f, 3f, 0, 20),  0.001f);
            Assert.AreEqual(3f, SkillScaling.Value(8f, 3f, 20, 20), 0.001f);
            Assert.AreEqual(5.5f, SkillScaling.Value(8f, 3f, 10, 20), 0.001f);
        }

        // SC3: Stufe wird auf [0, maxLevel] geklemmt.
        [Test]
        public void Value_ClampsLevel()
        {
            Assert.AreEqual(5f,  SkillScaling.Value(5f, 25f, -3, 20), 0.001f);
            Assert.AreEqual(25f, SkillScaling.Value(5f, 25f, 99, 20), 0.001f);
        }

        // SC4: Ganzzahliger Wert (Ladungen) rundet kaufmännisch.
        [Test]
        public void IntValue_Rounds()
        {
            // 1 -> 5 über 20 Stufen: Stufe 0 = 1, Stufe 10 = 3, Stufe 20 = 5
            Assert.AreEqual(1, SkillScaling.IntValue(1f, 5f, 0, 20));
            Assert.AreEqual(3, SkillScaling.IntValue(1f, 5f, 10, 20));
            Assert.AreEqual(5, SkillScaling.IntValue(1f, 5f, 20, 20));
        }

        // SC5: maxLevel <= 0 -> immer Startwert (kein Division-durch-0).
        [Test]
        public void Value_MaxLevelZero_ReturnsStart()
        {
            Assert.AreEqual(7f, SkillScaling.Value(7f, 99f, 5, 0), 0.001f);
        }
    }
}
