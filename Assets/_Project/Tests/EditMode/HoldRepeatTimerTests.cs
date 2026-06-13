using CozySanta.Core.Input;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class HoldRepeatTimerTests
    {
        private const float Delay = 0.4f;
        private const float Interval = 0.15f;

        // HR1: Erster Tastendruck löst sofort genau einmal aus.
        [Test]
        public void Tick_FirstPress_FiresOnce()
        {
            var timer = new HoldRepeatTimer();
            Assert.IsTrue(timer.Tick(true, 0f, Delay, Interval), "Druck löst sofort aus");
            Assert.IsFalse(timer.Tick(true, 0f, Delay, Interval), "kein zweiter Trigger ohne Zeit");
        }

        // HR2: Gehalten unter der Anfangsverzögerung -> keine Wiederholung.
        [Test]
        public void Tick_HeldBelowInitialDelay_DoesNotRepeat()
        {
            var timer = new HoldRepeatTimer();
            timer.Tick(true, 0f, Delay, Interval); // erster Treffer
            Assert.IsFalse(timer.Tick(true, 0.3f, Delay, Interval));
        }

        // HR3: Nach der Anfangsverzögerung beginnt die Wiederholung im Intervall.
        [Test]
        public void Tick_AfterInitialDelay_RepeatsAtInterval()
        {
            var timer = new HoldRepeatTimer();
            timer.Tick(true, 0f, Delay, Interval); // erster Treffer
            Assert.IsTrue(timer.Tick(true, Delay, Delay, Interval), "erste Wiederholung");
            Assert.IsFalse(timer.Tick(true, Interval * 0.5f, Delay, Interval));
            Assert.IsTrue(timer.Tick(true, Interval * 0.5f, Delay, Interval), "zweite Wiederholung");
        }

        // HR4: Loslassen setzt zurück -> nächster Druck löst wieder sofort aus.
        [Test]
        public void Tick_Release_ResetsAndFiresAgainOnNextPress()
        {
            var timer = new HoldRepeatTimer();
            timer.Tick(true, 0f, Delay, Interval);
            timer.Tick(true, Delay, Delay, Interval); // im Wiederhol-Modus
            Assert.IsFalse(timer.Tick(false, 1f, Delay, Interval), "Loslassen löst nicht aus");
            Assert.IsTrue(timer.Tick(true, 0f, Delay, Interval), "neuer Druck wieder sofort");
        }

        // HR5: Nicht-positive Schwelle schaltet die Auto-Wiederholung ab (nur Einzelauslösung).
        [Test]
        public void Tick_NonPositiveThreshold_NoAutoRepeat()
        {
            var timer = new HoldRepeatTimer();
            Assert.IsTrue(timer.Tick(true, 0f, 0f, 0f));
            Assert.IsFalse(timer.Tick(true, 10f, 0f, 0f));
        }
    }
}
