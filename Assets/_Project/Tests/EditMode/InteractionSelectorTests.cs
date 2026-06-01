using System.Collections.Generic;
using CozySanta.Core.Interaction;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>
    /// EditMode-/Unit-Tests für die reine Decide-Regel der Zielauswahl.
    /// Laufen ohne Szenenstart und ohne Unity-Laufzeit-APIs.
    /// </summary>
    public sealed class InteractionSelectorTests
    {
        private static SelectionSettings Settings() => new SelectionSettings(3f, 30f);

        // T1: keine Kandidaten -> None
        [Test]
        public void Decide_NoCandidates_ReturnsNone()
        {
            var result = InteractionSelector.Decide(new List<InteractionCandidate>(), Settings());
            Assert.IsFalse(result.HasTarget);
        }

        // T2: ein gültiger Kandidat -> wird gewählt
        [Test]
        public void Decide_SingleValidCandidate_IsSelected()
        {
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(7, 1.5f, 10f)
            };

            var result = InteractionSelector.Decide(candidates, Settings());

            Assert.IsTrue(result.HasTarget);
            Assert.AreEqual(7, result.TargetId);
        }

        // T3: mehrere gültige -> kleinste Distanz gewinnt
        [Test]
        public void Decide_MultipleValid_NearestWins()
        {
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(1, 2.5f, 5f),
                new InteractionCandidate(2, 1.0f, 20f),
                new InteractionCandidate(3, 2.0f, 5f)
            };

            var result = InteractionSelector.Decide(candidates, Settings());

            Assert.IsTrue(result.HasTarget);
            Assert.AreEqual(2, result.TargetId);
        }

        // T4: Distanzgleichstand -> kleinerer Winkel gewinnt
        [Test]
        public void Decide_DistanceTie_SmallerAngleWins()
        {
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(1, 2.0f, 25f),
                new InteractionCandidate(2, 2.0f, 8f)
            };

            var result = InteractionSelector.Decide(candidates, Settings());

            Assert.IsTrue(result.HasTarget);
            Assert.AreEqual(2, result.TargetId);
        }

        // T5: außerhalb MaxAngle -> ignoriert
        [Test]
        public void Decide_CandidateOutsideMaxAngle_IsIgnored()
        {
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(1, 1.0f, 45f) // Winkel > 30
            };

            var result = InteractionSelector.Decide(candidates, Settings());

            Assert.IsFalse(result.HasTarget);
        }

        // T6: außerhalb MaxRange -> ignoriert
        [Test]
        public void Decide_CandidateOutsideMaxRange_IsIgnored()
        {
            var candidates = new List<InteractionCandidate>
            {
                new InteractionCandidate(1, 5.0f, 5f) // Distanz > 3
            };

            var result = InteractionSelector.Decide(candidates, Settings());

            Assert.IsFalse(result.HasTarget);
        }
    }
}
