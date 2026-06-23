using System.Collections.Generic;
using CozySanta.Core.Rendering;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>
    /// EditMode-/Unit-Tests für die dichte Slot-Verwaltung des instanzierten Item-Renderings
    /// (Swap-with-last + Chunk-Bereiche). Reine Core-Logik, kein Szenenstart.
    /// </summary>
    public sealed class InstanceSlotsTests
    {
        // IS1: Add liefert fortlaufende Indizes; Count und OwnerAt stimmen.
        [Test]
        public void Add_ReturnsSequentialIndices()
        {
            var slots = new InstanceSlots();
            Assert.AreEqual(0, slots.Add(100));
            Assert.AreEqual(1, slots.Add(200));
            Assert.AreEqual(2, slots.Add(300));
            Assert.AreEqual(3, slots.Count);
            Assert.AreEqual(100, slots.OwnerAt(0));
            Assert.AreEqual(300, slots.OwnerAt(2));
        }

        // IS2: RemoveAt in der Mitte -> letztes Element rückt nach, gemeldeter Quell-Index = alter Letzter.
        [Test]
        public void RemoveAt_Middle_SwapsLastIntoGap()
        {
            var slots = new InstanceSlots();
            slots.Add(100); // 0
            slots.Add(200); // 1
            slots.Add(300); // 2

            var movedFrom = slots.RemoveAt(1);

            Assert.AreEqual(2, movedFrom);          // das ehemals letzte (Index 2) ist nachgerückt
            Assert.AreEqual(2, slots.Count);
            Assert.AreEqual(300, slots.OwnerAt(1)); // 300 sitzt jetzt im Loch
            Assert.AreEqual(100, slots.OwnerAt(0));
        }

        // IS3: RemoveAt des letzten Slots -> kein Nachrücken (-1), Dichte bleibt.
        [Test]
        public void RemoveAt_Last_NoSwap()
        {
            var slots = new InstanceSlots();
            slots.Add(100); // 0
            slots.Add(200); // 1

            var movedFrom = slots.RemoveAt(1);

            Assert.AreEqual(-1, movedFrom);
            Assert.AreEqual(1, slots.Count);
            Assert.AreEqual(100, slots.OwnerAt(0));
        }

        // IS4: RemoveAt mit ungültigem Index -> -1, keine Änderung.
        [Test]
        public void RemoveAt_InvalidIndex_NoOp()
        {
            var slots = new InstanceSlots();
            slots.Add(100);

            Assert.AreEqual(-1, slots.RemoveAt(5));
            Assert.AreEqual(-1, slots.RemoveAt(-1));
            Assert.AreEqual(1, slots.Count);
        }

        // IS5: ChunkRanges respektiert die 1023er-Grenze an den Randfällen 0/1/1023/1024/2046.
        [Test]
        public void ChunkRanges_RespectsBatchLimit()
        {
            CollectionAssert.IsEmpty(Ranges(0, 1023));
            CollectionAssert.AreEqual(new[] { (0, 1) }, Ranges(1, 1023));
            CollectionAssert.AreEqual(new[] { (0, 1023) }, Ranges(1023, 1023));
            CollectionAssert.AreEqual(new[] { (0, 1023), (1023, 1) }, Ranges(1024, 1023));
            CollectionAssert.AreEqual(new[] { (0, 1023), (1023, 1023) }, Ranges(2046, 1023));
        }

        private static List<(int start, int count)> Ranges(int total, int max)
        {
            var list = new List<(int, int)>();
            foreach (var r in InstanceSlots.ChunkRanges(total, max))
            {
                list.Add(r);
            }

            return list;
        }
    }
}
