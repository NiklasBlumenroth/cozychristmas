using CozySanta.Core.Keys;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für GateLockData (G1–G2).</summary>
    public sealed class GateLockDataTests
    {
        // ── G1: CanOpen false wenn Schlüssel fehlt ────────────────────────────────
        [Test]
        public void CanOpen_ReturnsFalse_WhenKeyMissing()
        {
            var inv  = new KeyInventory();
            inv.AddKey("poststelle");
            var gate = new GateLockData(new[] { "poststelle", "deko" });
            Assert.IsFalse(gate.CanOpen(inv));
        }

        // ── G2: CanOpen true wenn alle Schlüssel vorhanden ───────────────────────
        [Test]
        public void CanOpen_ReturnsTrue_WhenAllKeysPresent()
        {
            var inv  = new KeyInventory();
            inv.AddKey("poststelle");
            inv.AddKey("deko");
            var gate = new GateLockData(new[] { "poststelle", "deko" });
            Assert.IsTrue(gate.CanOpen(inv));
        }
    }
}
