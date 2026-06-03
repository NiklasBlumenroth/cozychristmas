using CozySanta.Core.Progression;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für XpLedger (X1–X4), SkillSet/Skill (S1–S5) und Skillwerte (V1).</summary>
    public sealed class ProgressionTests
    {
        private static SkillConfig[] Configs() => new[]
        {
            new SkillConfig(1.0f, 0.1f, 3.0f,  20),        // LampPower
            new SkillConfig(0.8f, 0.05f, 1.8f, 20),        // LampCone
            new SkillConfig(12f,  1.0f,  32f,  20),        // LampBattery
            new SkillConfig(5f,   1.0f,  25f,  20),        // CarryCapacity
            new SkillConfig(3f,   0.15f, 6f,   20),        // MoveSpeed
            new SkillConfig(0f,   1f,    20f,  20, true),  // SortVision (unlockable)
            new SkillConfig(0f,   1f,    20f,  20, true),  // ObjectPull (unlockable)
        };

        // ── X1: XP unter Schwelle → Level/Punkte unverändert ────────────────────
        [Test]
        public void AddXp_BelowThreshold_LevelUnchanged()
        {
            var ledger = new XpLedger();
            ledger.Add(99);
            Assert.AreEqual(0, ledger.Level);
            Assert.AreEqual(0, ledger.EarnedSkillPoints);
            Assert.AreEqual(99, ledger.XpIntoLevel);
        }

        // ── X2: XP überschreitet Schwelle → Level +1, Punkte vergeben ───────────
        [Test]
        public void AddXp_ReachesThreshold_LevelIncreases()
        {
            var ledger = new XpLedger();
            ledger.Add(100);
            Assert.AreEqual(1, ledger.Level);
            Assert.AreEqual(1, ledger.EarnedSkillPoints);
            Assert.AreEqual(0, ledger.XpIntoLevel);
        }

        // ── X3: Sehr viel XP → mehrere Level, korrekte Punktsumme ───────────────
        [Test]
        public void AddXp_MultipleLevels_AllPointsAwarded()
        {
            var ledger = new XpLedger();
            ledger.Add(500);
            Assert.AreEqual(5, ledger.Level);
            Assert.AreEqual(5, ledger.EarnedSkillPoints);
        }

        // ── X4: XP 0 oder negativ → kein Effekt ─────────────────────────────────
        [Test]
        public void AddXp_ZeroOrNegative_NoEffect()
        {
            var ledger = new XpLedger();
            ledger.Add(0);
            ledger.Add(-50);
            Assert.AreEqual(0, ledger.Level);
            Assert.AreEqual(0, ledger.TotalXp);
        }

        // ── S1: Invest mit Punkt → Stufe +1, Punkt verbraucht ───────────────────
        [Test]
        public void Invest_WithAvailablePoint_RaisesLevel()
        {
            var state = new ProgressionState(Configs());
            state.AwardXp(100); // Level 1 → 1 Punkt
            var before = state.Skills.Get(SkillId.LampPower).Level;
            Assert.IsTrue(state.Invest(SkillId.LampPower));
            Assert.AreEqual(before + 1, state.Skills.Get(SkillId.LampPower).Level);
            Assert.AreEqual(0, state.AvailablePoints);
        }

        // ── S2: Invest ohne Punkte → keine Änderung ─────────────────────────────
        [Test]
        public void Invest_NoPoints_Rejected()
        {
            var state = new ProgressionState(Configs());
            Assert.AreEqual(0, state.AvailablePoints);
            Assert.IsFalse(state.Invest(SkillId.LampPower));
            Assert.AreEqual(0, state.Skills.Get(SkillId.LampPower).Level);
        }

        // ── S3: Invest auf Maximalstufe → keine Änderung ────────────────────────
        [Test]
        public void Invest_AtMaxLevel_Rejected()
        {
            var state = new ProgressionState(Configs());
            state.AwardXp(9999);
            var skill = state.Skills.Get(SkillId.LampPower);
            while (state.AvailablePoints > 0 && skill.CanRaise)
                state.Invest(SkillId.LampPower);
            Assert.AreEqual(20, skill.Level);
            Assert.IsFalse(state.Invest(SkillId.LampPower));
        }

        // ── S4: Freischalt-Skill erste Investition → Unlocked = true ────────────
        [Test]
        public void Invest_UnlockableSkill_SetsUnlocked()
        {
            var state = new ProgressionState(Configs());
            state.AwardXp(100);
            var skill = state.Skills.Get(SkillId.SortVision);
            Assert.IsFalse(skill.IsUnlocked);
            state.Invest(SkillId.SortVision);
            Assert.IsTrue(skill.IsUnlocked);
            Assert.AreEqual(1, skill.Level);
        }

        // ── S5: AvailablePoints = verdient − ausgegeben (nie negativ) ────────────
        [Test]
        public void AvailablePoints_NeverNegative()
        {
            var state = new ProgressionState(Configs());
            state.AwardXp(100); // 1 Punkt
            state.Invest(SkillId.LampPower);
            Assert.AreEqual(0, state.AvailablePoints);
            Assert.IsFalse(state.Invest(SkillId.LampPower)); // kein weiterer Punkt
            Assert.AreEqual(0, state.AvailablePoints);
        }

        // ── V1: Skillwert = Basis + Schritt × Stufe, Deckelung greift ───────────
        [Test]
        public void SkillValue_ScalesWithLevel_AndClamps()
        {
            var state = new ProgressionState(Configs());
            state.AwardXp(9999);
            var skill = state.Skills.Get(SkillId.CarryCapacity); // base=5, step=1, max=25

            // Stufe 0: Basiswert
            Assert.AreEqual(5f, skill.Value, 0.001f);

            // Stufe 10: 5 + 1 × 10 = 15
            for (var i = 0; i < 10; i++) state.Invest(SkillId.CarryCapacity);
            Assert.AreEqual(15f, skill.Value, 0.001f);

            // Stufe 20 (Max): 5 + 1 × 20 = 25 (genau an der Grenze)
            for (var i = 0; i < 10; i++) state.Invest(SkillId.CarryCapacity);
            Assert.AreEqual(25f, skill.Value, 0.001f);
        }
    }
}
