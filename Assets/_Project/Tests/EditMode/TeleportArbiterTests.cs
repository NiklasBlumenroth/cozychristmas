using CozySanta.Core.Teleport;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für den Re-Entry-Schutz des Teleports (TP1–TP4).</summary>
    public sealed class TeleportArbiterTests
    {
        // ── TP1: Erstes Betreten eines freien Triggers löst aus ───────────────────
        [Test]
        public void ShouldTeleport_ReturnsTrue_OnFirstEnter()
        {
            var arbiter = new TeleportArbiter();
            Assert.IsTrue(arbiter.ShouldTeleport(0));
        }

        // ── TP2: Erneutes Betreten ohne Verlassen löst nicht aus (kein Doppelsprung)
        [Test]
        public void ShouldTeleport_ReturnsFalse_WhileStillOccupied()
        {
            var arbiter = new TeleportArbiter();
            arbiter.ShouldTeleport(0);
            Assert.IsFalse(arbiter.ShouldTeleport(0));
        }

        // ── TP3: Nach Verlassen löst der Trigger wieder aus ───────────────────────
        [Test]
        public void ShouldTeleport_ReturnsTrue_AfterExit()
        {
            var arbiter = new TeleportArbiter();
            arbiter.ShouldTeleport(0);
            arbiter.NotifyExit(0);
            Assert.IsTrue(arbiter.ShouldTeleport(0));
        }

        // ── TP4: Beim Ziel als belegt markierter Trigger schluckt das Lande-Enter ──
        //         und feuert erst nach Verlassen + erneutem Betreten wieder.
        [Test]
        public void MarkOccupied_SwallowsLandingEnter_ThenReArmsAfterExit()
        {
            var arbiter = new TeleportArbiter();

            // Spieler betritt Trigger 0, Ziel überlappt Trigger 1 → 1 wird vorbelegt.
            Assert.IsTrue(arbiter.ShouldTeleport(0));
            arbiter.MarkOccupied(1);

            // Lande-Enter auf Trigger 1 wird verschluckt (kein Rück-Bounce).
            Assert.IsFalse(arbiter.ShouldTeleport(1));

            // Spieler verlässt beide, betritt 1 dann bewusst → löst aus.
            arbiter.NotifyExit(0);
            arbiter.NotifyExit(1);
            Assert.IsTrue(arbiter.ShouldTeleport(1));
        }
    }
}
