using System.Collections.Generic;
using CozySanta.Core.Sorting;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>
    /// EditMode-Tests der reinen Truhen-Validierung (alles-oder-nichts + Verriegelung bei Soll-Menge).
    /// </summary>
    public sealed class GiftChestValidationTests
    {
        private static SortKey Auto => new SortKey("Toy Car");
        private static SortKey Boot => new SortKey("Toy Boat");

        private static List<SortKey> Keys(params SortKey[] keys) => new List<SortKey>(keys);

        // GC1 – Leerer Inhalt: korrekt, nimmt nichts an, verriegelt nicht.
        [Test]
        public void GC1_Empty_AcceptsNothing()
        {
            var d = GiftChestValidation.Decide(Keys(), Auto, alreadyAccepted: 0, required: 50);
            Assert.IsTrue(d.AllCorrect);
            Assert.AreEqual(0, d.AcceptCount);
            Assert.IsFalse(d.Locks);
            Assert.IsFalse(d.Reject);
        }

        // GC2 – Alle korrekt: nimmt alle an, verriegelt noch nicht.
        [Test]
        public void GC2_AllCorrect_AcceptsAll()
        {
            var d = GiftChestValidation.Decide(Keys(Auto, Auto, Auto), Auto, alreadyAccepted: 0, required: 50);
            Assert.IsTrue(d.AllCorrect);
            Assert.AreEqual(3, d.AcceptCount);
            Assert.IsFalse(d.Locks);
        }

        // GC3 – Ein falsches Item weist den GESAMTEN Inhalt ab (alles-oder-nichts).
        [Test]
        public void GC3_OneWrong_RejectsAll()
        {
            var d = GiftChestValidation.Decide(Keys(Auto, Boot, Auto), Auto, alreadyAccepted: 0, required: 50);
            Assert.IsFalse(d.AllCorrect);
            Assert.IsTrue(d.Reject);
            Assert.AreEqual(0, d.AcceptCount);
            Assert.IsFalse(d.Locks);
        }

        // GC4 – Erreichen der Soll-Menge verriegelt die Truhe.
        [Test]
        public void GC4_ReachingRequired_Locks()
        {
            var d = GiftChestValidation.Decide(Keys(Auto, Auto), Auto, alreadyAccepted: 48, required: 50);
            Assert.IsTrue(d.AllCorrect);
            Assert.AreEqual(2, d.AcceptCount);
            Assert.IsTrue(d.Locks);
        }

        // GC5 – Überschreiten der Soll-Menge verriegelt ebenfalls (>= required).
        [Test]
        public void GC5_OverReaching_Locks()
        {
            var d = GiftChestValidation.Decide(Keys(Auto, Auto, Auto), Auto, alreadyAccepted: 49, required: 50);
            Assert.IsTrue(d.Locks);
            Assert.AreEqual(3, d.AcceptCount);
        }

        // GC6 – required <= 0: verriegelt nie (offene Truhe / Fehlkonfiguration), nimmt aber an.
        [Test]
        public void GC6_NoRequired_NeverLocks()
        {
            var d = GiftChestValidation.Decide(Keys(Auto), Auto, alreadyAccepted: 0, required: 0);
            Assert.IsTrue(d.AllCorrect);
            Assert.AreEqual(1, d.AcceptCount);
            Assert.IsFalse(d.Locks);
        }
    }
}
