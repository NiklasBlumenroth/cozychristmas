using CozySanta.Core.Carry;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests des reinen Flug-Fortschritts (<see cref="FlightProgress"/>).</summary>
    public sealed class FlightProgressTests
    {
        // FP1 – Start: nichts verstrichen, nicht fertig, Fortschritt 0
        [Test]
        public void FP1_StartsAtZero()
        {
            var f = new FlightProgress(0.2f);
            Assert.AreEqual(0f, f.Linear01, 1e-5f);
            Assert.AreEqual(0f, f.Eased, 1e-5f);
            Assert.IsFalse(f.IsDone);
        }

        // FP2 – Halbe Dauer: linear 0.5, Smoothstep(0.5) = 0.5, noch nicht fertig
        [Test]
        public void FP2_Halfway()
        {
            var f = new FlightProgress(0.2f);
            f.Step(0.1f);
            Assert.AreEqual(0.5f, f.Linear01, 1e-5f);
            Assert.AreEqual(0.5f, f.Eased, 1e-5f);
            Assert.IsFalse(f.IsDone);
        }

        // FP3 – Über das Ziel hinaus: linear/Eased klemmen bei 1, IsDone
        [Test]
        public void FP3_OvershootClamps()
        {
            var f = new FlightProgress(0.2f);
            f.Step(1f);
            Assert.AreEqual(1f, f.Linear01, 1e-5f);
            Assert.AreEqual(1f, f.Eased, 1e-5f);
            Assert.IsTrue(f.IsDone);
        }

        // FP4 – Smoothstep: Endpunkte exakt, ease-in/-out (früh < linear, spät > linear), monoton
        [Test]
        public void FP4_EasingShape()
        {
            var a = new FlightProgress(1f);
            a.Step(0.25f);
            Assert.Less(a.Eased, 0.25f); // ease-in: langsamer Start
            Assert.Greater(a.Eased, 0f);

            var b = new FlightProgress(1f);
            b.Step(0.75f);
            Assert.Greater(b.Eased, 0.75f); // ease-out: schnelle Mitte, sanftes Ende
        }

        // FP5 – Nicht-positives Delta ändert nichts
        [Test]
        public void FP5_NonPositiveStepIsNoOp()
        {
            var f = new FlightProgress(0.2f);
            f.Step(0f);
            f.Step(-1f);
            Assert.AreEqual(0f, f.Linear01, 1e-5f);
            Assert.IsFalse(f.IsDone);
        }
    }
}
