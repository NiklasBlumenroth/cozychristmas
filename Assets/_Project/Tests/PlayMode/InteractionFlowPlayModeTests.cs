using System.Collections;
using System.Collections.Generic;
using CozySanta.Core.Interaction;
using CozySanta.Runtime.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CozySanta.Tests.PlayMode
{
    /// <summary>
    /// PlayMode-E2E-Test des Decide/Apply-Flows in einer Minimalszene mit Fake-Provider.
    /// Bestätigt, dass die Runtime das Core-Ergebnis korrekt anwendet (Apply) und konsistent
    /// zum EditMode-Test ist.
    /// </summary>
    public sealed class InteractionFlowPlayModeTests
    {
        private sealed class FakeProbe : IInteractionProbe
        {
            public readonly List<InteractionCandidate> Candidates = new List<InteractionCandidate>();
            public IReadOnlyList<InteractionCandidate> QueryCandidates() => Candidates;
        }

        // T7: Fake-Probe mit einem gültigen Ziel -> Controller setzt FocusedTargetId
        [UnityTest]
        public IEnumerator Apply_SetsFocusedTarget_FromCoreDecision()
        {
            var go = new GameObject("player");
            var controller = go.AddComponent<PlayerInteractionController>();

            var probe = new FakeProbe();
            probe.Candidates.Add(new InteractionCandidate(42, 1.0f, 5f));
            controller.Configure(probe);

            // Ein Frame, damit der PlayMode-Lifecycle (Awake/Update) anläuft.
            yield return null;

            // Deterministischer Durchlauf für die Assertion.
            controller.Tick();

            Assert.IsTrue(controller.HasFocus);
            Assert.AreEqual(42, controller.FocusedTargetId);

            Object.Destroy(go);
        }

        // E2E-Gegenprobe: kein Ziel in Reichweite -> kein Fokus
        [UnityTest]
        public IEnumerator Apply_NoCandidate_NoFocus()
        {
            var go = new GameObject("player");
            var controller = go.AddComponent<PlayerInteractionController>();

            controller.Configure(new FakeProbe());

            yield return null;
            controller.Tick();

            Assert.IsFalse(controller.HasFocus);

            Object.Destroy(go);
        }
    }
}
