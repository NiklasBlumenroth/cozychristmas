using CozySanta.Core.Interaction;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class InteractionTriggerTests
    {
        // G1: Fokus + Tastendruck -> true
        [Test]
        public void ShouldInteract_FocusAndPressed_True()
        {
            Assert.IsTrue(InteractionTrigger.ShouldInteract(true, true));
        }

        // G2: fehlender Fokus ODER kein Tastendruck -> false
        [Test]
        public void ShouldInteract_MissingFocusOrPress_False()
        {
            Assert.IsFalse(InteractionTrigger.ShouldInteract(false, true));
            Assert.IsFalse(InteractionTrigger.ShouldInteract(true, false));
            Assert.IsFalse(InteractionTrigger.ShouldInteract(false, false));
        }
    }
}
