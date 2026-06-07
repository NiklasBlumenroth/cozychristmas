using CozySanta.Core.Snow;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>
    /// EditMode-Tests der reinen F5-Schmelzlogik: Akku (B1–B4) und Höhenfeld/Coverage (M1–M5).
    /// </summary>
    public sealed class SnowMeltTests
    {
        // B1 – Akku startet voll, kann schmelzen
        [Test]
        public void B1_Battery_StartsFull_CanMelt()
        {
            var battery = new LampBattery(10f);
            Assert.AreEqual(10f, battery.Level);
            Assert.IsTrue(battery.CanMelt);
            Assert.AreEqual(1f, battery.Fraction);
        }

        // B2 – Verbrauch senkt, clamp bei 0, leer sperrt
        [Test]
        public void B2_Battery_DrainClampsAndBlocks()
        {
            var battery = new LampBattery(5f);
            battery.Drain(3f);
            Assert.AreEqual(2f, battery.Level);
            battery.Drain(10f);
            Assert.AreEqual(0f, battery.Level);
            Assert.IsFalse(battery.CanMelt);
        }

        // B3 – Aufladen ermöglicht erneutes Schmelzen, clamp bei Capacity
        [Test]
        public void B3_Battery_RechargeClampsAndUnblocks()
        {
            var battery = new LampBattery(5f);
            battery.Drain(5f);
            Assert.IsFalse(battery.CanMelt);
            battery.Recharge(2f);
            Assert.IsTrue(battery.CanMelt);
            battery.Recharge(100f);
            Assert.AreEqual(5f, battery.Level);
        }

        // B4 – Refill setzt auf voll
        [Test]
        public void B4_Battery_Refill()
        {
            var battery = new LampBattery(8f);
            battery.Drain(8f);
            battery.Refill();
            Assert.AreEqual(8f, battery.Level);
        }

        // M1 – Feld startet voll, Coverage 0
        [Test]
        public void M1_Field_StartsFull_ZeroCoverage()
        {
            var field = new MeltField(4);
            Assert.AreEqual(16, field.CellCount);
            Assert.AreEqual(0f, field.Coverage);
            Assert.AreEqual(1f, field.HeightAt(2, 2));
        }

        // M2 – Schmelzen senkt die Mitte und clamp bei 0
        [Test]
        public void M2_Melt_LowersCenter_ClampsAtZero()
        {
            var field = new MeltField(5);
            field.Melt(0.5f, 0.5f, 0.5f, 2f); // Mitte: falloff 1, delta -2 -> clamp 0
            Assert.AreEqual(0f, field.HeightAt(2, 2));
            Assert.Greater(field.Coverage, 0f);
        }

        // M3 – Volle Freilegung ergibt 100 %
        [Test]
        public void M3_FullMelt_Gives100Percent()
        {
            var field = new MeltField(6);
            for (var i = 0; i < 30; i++)
            {
                field.Melt(0.5f, 0.5f, 1.5f, 1f);
            }

            Assert.AreEqual(1f, field.Coverage);
            Assert.AreEqual(100f, field.CoveragePercent);
        }

        // M4 – Auftragen hebt die Höhe wieder, clamp bei 1
        [Test]
        public void M4_Add_RaisesHeight_ClampsAtMax()
        {
            var field = new MeltField(5);
            field.Melt(0.5f, 0.5f, 0.5f, 1f);
            Assert.Less(field.HeightAt(2, 2), 1f);
            for (var i = 0; i < 10; i++)
            {
                field.Add(0.5f, 0.5f, 0.5f, 1f);
            }

            Assert.AreEqual(1f, field.HeightAt(2, 2));
            Assert.AreEqual(0f, field.Coverage);
        }

        // M5 – Außerhalb des Pinsels bleibt unverändert
        [Test]
        public void M5_Melt_OutsideRadius_Unaffected()
        {
            var field = new MeltField(5);
            field.Melt(0f, 0f, 0.2f, 2f); // Ecke (0,0)
            Assert.AreEqual(1f, field.HeightAt(4, 4), "Gegenüberliegende Ecke bleibt voll.");
        }

        // M6 – Elliptischer Pinsel: getrennte UV-Radien wirken nur entlang der jeweiligen Achse
        [Test]
        public void M6_Melt_EllipticalBrush_RespectsPerAxisRadius()
        {
            var field = new MeltField(9); // Mitte = (4,4), u=v=0.5; last=8 -> rx=0.8, ry=3.6 Zellen
            // Schmaler X-Radius, breiter Y-Radius: Punkt entlang Y wird erfasst, gleich weit in X nicht.
            field.Melt(0.5f, 0.5f, 0.1f, 0.45f, 2f);
            Assert.Less(field.HeightAt(4, 2), 1f, "Innerhalb des großen Y-Radius -> abgesenkt.");
            Assert.AreEqual(1f, field.HeightAt(0, 4), "Außerhalb des kleinen X-Radius -> unverändert.");
        }
    }
}
