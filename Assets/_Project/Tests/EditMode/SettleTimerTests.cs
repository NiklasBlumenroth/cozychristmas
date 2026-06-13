using CozySanta.Core.Items;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class SettleTimerTests
    {
        private const float Lin = 0.05f;
        private const float Ang = 0.2f;
        private const float Settle = 0.5f;

        // ST1: Ruhig über die volle Settle-Dauer -> meldet Ruhe.
        [Test]
        public void Tick_CalmForSettleDuration_ReportsRest()
        {
            var t = new SettleTimer();
            Assert.IsFalse(t.Tick(0f, 0f, 0.3f, Lin, Ang, Settle), "noch unter der Dauer");
            Assert.IsTrue(t.Tick(0f, 0f, 0.2f, Lin, Ang, Settle), "Dauer erreicht");
        }

        // ST2: Bewegung über der Linearschwelle setzt den Zähler zurück.
        [Test]
        public void Tick_LinearMovement_ResetsCalm()
        {
            var t = new SettleTimer();
            t.Tick(0f, 0f, 0.4f, Lin, Ang, Settle);          // fast ruhig
            Assert.IsFalse(t.Tick(1f, 0f, 0.4f, Lin, Ang, Settle), "Bewegung -> Reset");
            Assert.IsFalse(t.Tick(0f, 0f, 0.4f, Lin, Ang, Settle), "Zähler beginnt neu");
        }

        // ST3: Reine Drehung über der Winkelschwelle hält ebenfalls wach.
        [Test]
        public void Tick_AngularMovement_KeepsAwake()
        {
            var t = new SettleTimer();
            Assert.IsFalse(t.Tick(0f, 1f, 1f, Lin, Ang, Settle));
        }

        // ST4: Genau auf der Schwelle gilt noch als ruhig (<=).
        [Test]
        public void Tick_ExactlyOnThreshold_CountsAsCalm()
        {
            var t = new SettleTimer();
            Assert.IsTrue(t.Tick(Lin, Ang, Settle, Lin, Ang, Settle));
        }

        // ST5: Reset verwirft die bisher gesammelte ruhige Zeit.
        [Test]
        public void Reset_DiscardsAccumulatedCalm()
        {
            var t = new SettleTimer();
            t.Tick(0f, 0f, 0.4f, Lin, Ang, Settle);
            t.Reset();
            Assert.IsFalse(t.Tick(0f, 0f, 0.2f, Lin, Ang, Settle), "nach Reset wieder von vorn");
        }
    }
}
