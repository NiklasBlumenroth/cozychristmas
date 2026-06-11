using CozySanta.Core.Sorting;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests der Container-Füllreihenfolge je x-Spalte (<see cref="SlotFillOrder"/>).</summary>
    public sealed class SlotFillOrderTests
    {
        // FO1 – Leere Spalte: erster Slot = unten (y0), hinten (z max)
        [Test]
        public void FO1_Empty_FirstCell_BottomRear()
        {
            var occ = new bool[2, 2, 2];
            Assert.IsTrue(SlotFillOrder.TryNextFree(occ, 0, out var y, out var z));
            Assert.AreEqual((0, 1), (y, z));
        }

        // FO2 – Füllreihenfolge je Spalte: unten zuerst über die Tiefe, dann nächste Reihe darüber
        [Test]
        public void FO2_FillOrder_RearToFront_ThenUp()
        {
            var occ = new bool[2, 2, 2];
            var expected = new[] { (0, 1), (0, 0), (1, 1), (1, 0) }; // (y,z): y0 hinten→vorne, dann y1
            foreach (var cell in expected)
            {
                Assert.IsTrue(SlotFillOrder.TryNextFree(occ, 0, out var y, out var z));
                Assert.AreEqual(cell, (y, z));
                occ[0, y, z] = true;
            }

            Assert.IsFalse(SlotFillOrder.TryNextFree(occ, 0, out _, out _)); // Spalte voll
        }

        // FO3 – x bleibt fix: Füllen von Spalte 0 lässt Spalte 1 unberührt
        [Test]
        public void FO3_ColumnIsolated()
        {
            var occ = new bool[2, 2, 2];
            occ[0, 0, 1] = true; occ[0, 0, 0] = true; occ[0, 1, 1] = true; occ[0, 1, 0] = true; // Spalte 0 voll
            Assert.IsFalse(SlotFillOrder.TryNextFree(occ, 0, out _, out _));
            Assert.IsTrue(SlotFillOrder.TryNextFree(occ, 1, out var y, out var z)); // Spalte 1 noch frei
            Assert.AreEqual((0, 1), (y, z));
        }

        // FO4 – Entnahme spiegelverkehrt: oben/vorne zuerst
        [Test]
        public void FO4_Remove_TopFront_First()
        {
            var occ = new bool[1, 2, 2];
            occ[0, 0, 1] = true; // bottom-rear
            occ[0, 1, 0] = true; // top-front
            Assert.IsTrue(SlotFillOrder.TryNextOccupied(occ, 0, out var y, out var z));
            Assert.AreEqual((1, 0), (y, z));
        }
    }
}
