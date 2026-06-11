using CozySanta.Core.Props;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests der reinen Tür-Toggle-Logik (<see cref="DoorMotion"/>).</summary>
    public sealed class DoorMotionTests
    {
        // D1 – Start geschlossen, ruhend
        [Test]
        public void D1_StartsClosed()
        {
            var d = new DoorMotion(1f);
            Assert.AreEqual(DoorPhase.Closed, d.Phase);
            Assert.AreEqual(0f, d.Progress01);
            Assert.IsFalse(d.IsMoving);
        }

        // D2 – Toggle öffnet; Step erreicht offen und rastet bei 1 ein
        [Test]
        public void D2_Toggle_Opens_AndClamps()
        {
            var d = new DoorMotion(1f);
            d.Toggle();
            Assert.AreEqual(DoorPhase.Opening, d.Phase);
            d.Step(0.5f);
            Assert.AreEqual(0.5f, d.Progress01, 1e-4f);
            d.Step(1f); // über das Ziel hinaus
            Assert.AreEqual(1f, d.Progress01, 1e-4f);
            Assert.AreEqual(DoorPhase.Open, d.Phase);
            Assert.IsTrue(d.IsOpen);
            Assert.IsFalse(d.IsMoving);
        }

        // D3 – Toggle aus „offen" schließt wieder bis 0
        [Test]
        public void D3_Toggle_FromOpen_Closes()
        {
            var d = new DoorMotion(1f);
            d.Toggle(); d.Step(2f);            // ganz auf
            d.Toggle();
            Assert.AreEqual(DoorPhase.Closing, d.Phase);
            d.Step(2f);
            Assert.AreEqual(0f, d.Progress01, 1e-4f);
            Assert.AreEqual(DoorPhase.Closed, d.Phase);
        }

        // D4 – Toggle mitten in der Bewegung kehrt die Richtung um, ohne zu springen
        [Test]
        public void D4_Toggle_MidMotion_Reverses()
        {
            var d = new DoorMotion(1f);
            d.Toggle(); d.Step(0.4f);          // 0.4 offen, noch Opening
            d.Toggle();                         // umkehren
            Assert.AreEqual(DoorPhase.Closing, d.Phase);
            Assert.AreEqual(0.4f, d.Progress01, 1e-4f); // kein Sprung
            d.Step(0.4f);
            Assert.AreEqual(0f, d.Progress01, 1e-4f);
            Assert.AreEqual(DoorPhase.Closed, d.Phase);
        }
    }
}
