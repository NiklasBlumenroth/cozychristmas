using CozySanta.Core.Sorting;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>
    /// EditMode-Tests der reinen F4-Sortierlogik (S1–S10 aus data-model.md).
    /// </summary>
    public sealed class SortTargetTests
    {
        private static SortKey Europe => new SortKey("Europe", "Rot", "Stern");
        private static SortKey Asia => new SortKey("Asia", "Blau", "Teddy");

        // S1 – SortKey-Wertegleichheit
        [Test]
        public void S1_SortKey_Equality_ByValue()
        {
            Assert.IsTrue(new SortKey("Europe", "Rot", "Stern").Matches(Europe));
            Assert.IsFalse(Europe.Matches(Asia));
            Assert.IsFalse(Europe.Matches(new SortKey("Europe", "Rot")));
            Assert.AreEqual(Europe.GetHashCode(), new SortKey("Europe", "Rot", "Stern").GetHashCode());
        }

        // S2 – Classify korrekt/falsch
        [Test]
        public void S2_Classify_CorrectAndWrong()
        {
            var target = new SortTarget(Europe, 3);
            Assert.IsTrue(target.Classify(Europe));
            Assert.IsFalse(target.Classify(Asia));
        }

        // S3 – TryPlace legt oben auf, Zähler konsistent
        [Test]
        public void S3_TryPlace_UpdatesCounts()
        {
            var target = new SortTarget(Europe, 3);
            Assert.IsTrue(target.TryPlace(1, Europe));
            Assert.IsTrue(target.TryPlace(2, Asia));
            Assert.AreEqual(2, target.Count);
            Assert.AreEqual(1, target.CorrectCount);
            Assert.AreEqual(1, target.WrongCount);
        }

        // S4 – Vollständigkeit an der Grenze
        [Test]
        public void S4_Completion_AtRequiredCount()
        {
            var target = new SortTarget(Europe, 2);
            target.TryPlace(1, Europe);
            Assert.AreEqual(SortTargetState.Teilweise, target.Evaluate());
            target.TryPlace(2, Europe);
            Assert.AreEqual(SortTargetState.Vollstaendig, target.Evaluate());
            Assert.IsTrue(target.IsClosed);
        }

        // S5 – Falsch enthalten hält den Abschluss fern
        [Test]
        public void S5_WrongItem_BlocksCompletion()
        {
            var target = new SortTarget(Europe, 2);
            target.TryPlace(1, Europe);
            target.TryPlace(2, Europe);   // erreicht 2 korrekte? -> würde schließen, daher erst falsch testen
            Assert.IsTrue(target.IsClosed); // 2 korrekte, 0 falsch -> abgeschlossen

            var withWrong = new SortTarget(Europe, 2);
            withWrong.TryPlace(1, Europe);
            withWrong.TryPlace(2, Asia);   // falsch
            withWrong.TryPlace(3, Europe); // jetzt 2 korrekte, aber 1 falsch
            Assert.AreEqual(SortTargetState.FalschEnthalten, withWrong.Evaluate());
            Assert.IsFalse(withWrong.IsClosed);
        }

        // S6 – Teilweise
        [Test]
        public void S6_Partial_State()
        {
            var target = new SortTarget(Europe, 3);
            target.TryPlace(1, Europe);
            Assert.AreEqual(SortTargetState.Teilweise, target.Evaluate());
            Assert.IsFalse(target.IsClosed);
        }

        // S7 – LIFO-Entnahme, Zähler sinken
        [Test]
        public void S7_RemoveTop_LIFO()
        {
            var target = new SortTarget(Europe, 5);
            target.TryPlace(10, Europe);
            target.TryPlace(20, Asia);

            Assert.IsTrue(target.TryRemoveTop(out var first));
            Assert.AreEqual(20, first);   // zuletzt eingelegt zuerst
            Assert.AreEqual(0, target.WrongCount);

            Assert.IsTrue(target.TryRemoveTop(out var second));
            Assert.AreEqual(10, second);
            Assert.AreEqual(0, target.CorrectCount);
            Assert.AreEqual(0, target.Count);
        }

        // S8 – Entnahme aus leerem Fach
        [Test]
        public void S8_RemoveTop_Empty_ReturnsFalse()
        {
            var target = new SortTarget(Europe, 2);
            Assert.IsFalse(target.TryRemoveTop(out _));
        }

        // S9 – Sperre nach Abschluss
        [Test]
        public void S9_Closed_LocksMutations()
        {
            var target = new SortTarget(Europe, 1);
            Assert.IsTrue(target.TryPlace(1, Europe)); // schließt sofort (1 korrekt, 0 falsch)
            Assert.IsTrue(target.IsClosed);
            Assert.IsFalse(target.TryPlace(2, Europe));
            Assert.IsFalse(target.TryRemoveTop(out _));
            Assert.IsFalse(target.TryPeekTop(out _));
        }

        // S10 – JustCompleted nur im Abschluss-Schritt
        [Test]
        public void S10_JustCompleted_OnlyOnCompletionStep()
        {
            var target = new SortTarget(Europe, 2);
            target.TryPlace(1, Europe);
            Assert.IsFalse(target.JustCompleted);
            target.TryPlace(2, Europe);
            Assert.IsTrue(target.JustCompleted);
            // danach keine Mutation mehr möglich -> bleibt nicht erneut true
            target.TryPlace(3, Europe);
            Assert.IsFalse(target.JustCompleted);
        }
    }
}
