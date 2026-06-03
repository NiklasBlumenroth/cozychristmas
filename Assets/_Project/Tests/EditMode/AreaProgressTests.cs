using CozySanta.Core.Progression;
using CozySanta.Core.Snow;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für AreaTask/AreaProgress (A1–A3, B1–B2) und Lade-Basislogik (C1–C2).</summary>
    public sealed class AreaProgressTests
    {
        private static AreaProgress MakeProgress(int sortRequired = 3, float meltRequired = 80f)
        {
            var tasks = new[]
            {
                new AreaTask("sortieren", TaskType.Sort, "Fächer sortieren", sortRequired),
                new AreaTask("schmelzen", TaskType.Melt, "Schnee schmelzen", meltRequired),
            };
            return new AreaProgress(new AreaDefinition("Poststelle", tasks, 200));
        }

        // ── A1: Sort-Fortschritt steigt; IsComplete bei Grenze ──────────────────
        [Test]
        public void BookSort_IncreasesCount_CompletesAtRequired()
        {
            var p = MakeProgress(sortRequired: 3);
            p.BookSort("sortieren");
            Assert.AreEqual(1f, p.Tasks[0].Current, 0.001f);
            Assert.IsFalse(p.Tasks[0].IsComplete);

            p.BookSort("sortieren");
            p.BookSort("sortieren");
            Assert.IsTrue(p.Tasks[0].IsComplete);
        }

        // ── A2: Melt-Fortschritt steigt korrekt ─────────────────────────────────
        [Test]
        public void BookMelt_IncreasesPercent()
        {
            var p = MakeProgress(meltRequired: 80f);
            p.BookMelt("schmelzen", 30f);
            Assert.AreEqual(30f, p.Tasks[1].Current, 0.001f);
            p.BookMelt("schmelzen", 50f);
            Assert.AreEqual(80f, p.Tasks[1].Current, 0.001f);
            Assert.IsTrue(p.Tasks[1].IsComplete);
        }

        // ── A3: Buchung nach Task-Abschluss wird ignoriert ───────────────────────
        [Test]
        public void Book_AfterComplete_Ignored()
        {
            var p = MakeProgress(sortRequired: 1);
            p.BookSort("sortieren");
            Assert.IsTrue(p.Tasks[0].IsComplete);
            p.BookSort("sortieren");
            Assert.AreEqual(1f, p.Tasks[0].Current, 0.001f);
        }

        // ── B1: Alle Tasks erledigt → IsComplete true, OnCompleted einmalig ──────
        [Test]
        public void AllTasksDone_AreaComplete_EventFiredOnce()
        {
            var p = MakeProgress(sortRequired: 1, meltRequired: 10f);
            var fired = 0;
            p.OnCompleted += () => fired++;

            p.BookSort("sortieren");
            Assert.IsFalse(p.IsComplete);

            p.BookMelt("schmelzen", 10f);
            Assert.IsTrue(p.IsComplete);
            Assert.IsTrue(p.Completed);
            Assert.AreEqual(1, fired);
        }

        // ── B2: Weiterer Fortschritt nach Abschluss → kein zweites Event ─────────
        [Test]
        public void FurtherProgress_AfterCompletion_NoSecondEvent()
        {
            var p = MakeProgress(sortRequired: 1, meltRequired: 10f);
            var fired = 0;
            p.OnCompleted += () => fired++;

            p.BookSort("sortieren");
            p.BookMelt("schmelzen", 10f);
            p.BookSort("sortieren");
            p.BookMelt("schmelzen", 5f);

            Assert.AreEqual(1, fired);
            Assert.IsTrue(p.Completed);
        }

        // ── C1: LampBattery Recharge akkumuliert korrekt ─────────────────────────
        [Test]
        public void LampBattery_Recharge_Accumulates()
        {
            var battery = new LampBattery(10f);
            battery.Drain(10f);
            Assert.AreEqual(0f, battery.Level, 0.001f);

            battery.Recharge(3f);
            Assert.AreEqual(3f, battery.Level, 0.001f);
            battery.Recharge(3f);
            Assert.AreEqual(6f, battery.Level, 0.001f);
        }

        // ── C2: LampBattery clamp bei Kapazität (Ladestation stoppt bei voll) ────
        [Test]
        public void LampBattery_Recharge_ClampsAtCapacity()
        {
            var battery = new LampBattery(10f);
            battery.Drain(10f);
            battery.Recharge(15f);
            Assert.AreEqual(10f, battery.Level, 0.001f);
        }
    }
}
