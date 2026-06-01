using System.Collections;
using System.Collections.Generic;
using CozySanta.Core.Interaction;
using CozySanta.Runtime.Interaction;
using CozySanta.Runtime.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CozySanta.Tests.PlayMode
{
    /// <summary>
    /// PlayMode-E2E-Tests für F2: Bewegung gegen Wand sowie Fokus/Prompt/Interaktion über Fakes.
    /// </summary>
    public sealed class FirstPersonInteractionPlayModeTests
    {
        private sealed class FakeInteractable : IInteractable
        {
            public bool Interacted;
            public string PromptText => "Test";
            public void Interact() => Interacted = true;
        }

        private sealed class FakeProbeResolver : IInteractionProbe, IInteractableResolver
        {
            public readonly List<InteractionCandidate> Candidates = new List<InteractionCandidate>();
            public readonly Dictionary<int, IInteractable> Map = new Dictionary<int, IInteractable>();

            public IReadOnlyList<InteractionCandidate> QueryCandidates() => Candidates;

            public bool TryResolve(int targetId, out IInteractable interactable)
                => Map.TryGetValue(targetId, out interactable);
        }

        // E1: Bewegung gegen Wand -> keine Durchdringung
        [UnityTest]
        public IEnumerator Movement_StopsAtWall()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, 0f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1f, 2f);
            wall.transform.localScale = new Vector3(5f, 5f, 0.5f);

            var player = new GameObject("player");
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            player.AddComponent<CharacterController>();
            var fpc = player.AddComponent<FirstPersonController>();
            fpc.SetMoveInput(new Vector2(0f, 1f)); // vorwärts

            for (var i = 0; i < 150; i++)
            {
                yield return null;
            }

            // Wandvorderkante bei z = 1.75; Spieler darf nicht hindurch, soll aber vorwärts gekommen sein.
            Assert.Less(player.transform.position.z, 1.75f, "Spieler ist durch die Wand gelaufen.");
            Assert.Greater(player.transform.position.z, 0.2f, "Spieler hat sich nicht vorwärts bewegt.");

            Object.Destroy(player);
            Object.Destroy(wall);
            Object.Destroy(floor);
        }

        // E2/E3: Fokus auf Interactable -> Prompt sichtbar; kein Kandidat -> kein Fokus, Prompt versteckt
        [UnityTest]
        public IEnumerator Focus_TogglesPromptAndInteractableFocus()
        {
            var go = new GameObject("controller");
            var controller = go.AddComponent<PlayerInteractionController>();
            var presenter = go.AddComponent<InteractionPromptPresenter>();

            var fake = new FakeInteractable();
            var probe = new FakeProbeResolver();
            probe.Candidates.Add(new InteractionCandidate(99, 1f, 5f));
            probe.Map[99] = fake;
            controller.Configure(probe, probe, presenter);

            yield return null;
            controller.Tick();
            Assert.IsTrue(controller.HasInteractableFocus, "Erwartet: Fokus auf Interactable.");
            Assert.IsTrue(presenter.IsShown, "Erwartet: Hinweis sichtbar.");

            // E3: keine Kandidaten mehr
            probe.Candidates.Clear();
            probe.Map.Clear();
            controller.Tick();
            Assert.IsFalse(controller.HasInteractableFocus, "Erwartet: kein Fokus.");
            Assert.IsFalse(presenter.IsShown, "Erwartet: Hinweis versteckt.");

            Object.Destroy(go);
        }

        // E4/E5: mit Fokus -> Interact ausgelöst; ohne Fokus -> keine Auslösung
        [UnityTest]
        public IEnumerator Interact_OnlyWhenFocused()
        {
            var go = new GameObject("controller");
            var controller = go.AddComponent<PlayerInteractionController>();

            var fake = new FakeInteractable();
            var probe = new FakeProbeResolver();
            controller.Configure(probe, probe);

            // E5: kein Fokus -> keine Auslösung
            controller.Tick();
            controller.TryInteract();
            Assert.IsFalse(fake.Interacted, "Ohne Fokus darf nichts ausgelöst werden.");

            // E4: Fokus -> Auslösung genau am fokussierten Objekt
            probe.Candidates.Add(new InteractionCandidate(7, 1f, 3f));
            probe.Map[7] = fake;
            controller.Tick();
            controller.TryInteract();
            Assert.IsTrue(fake.Interacted, "Mit Fokus muss Interact ausgelöst werden.");

            yield return null;
            Object.Destroy(go);
        }
    }
}
