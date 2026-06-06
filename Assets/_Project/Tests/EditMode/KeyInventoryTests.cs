using CozySanta.Core.Keys;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für KeyInventory (K1–K4).</summary>
    public sealed class KeyInventoryTests
    {
        // ── K1: AddKey fügt Schlüssel hinzu ──────────────────────────────────────
        [Test]
        public void AddKey_MakesKeyAvailable()
        {
            var inv = new KeyInventory();
            inv.AddKey("poststelle");
            Assert.IsTrue(inv.HasKeys(new[] { "poststelle" }));
        }

        // ── K2: HasKeys false wenn ein Key fehlt ──────────────────────────────────
        [Test]
        public void HasKeys_ReturnsFalse_WhenOneKeyMissing()
        {
            var inv = new KeyInventory();
            inv.AddKey("poststelle");
            Assert.IsFalse(inv.HasKeys(new[] { "poststelle", "deko" }));
        }

        // ── K3: HasKeys true wenn alle Keys vorhanden ─────────────────────────────
        [Test]
        public void HasKeys_ReturnsTrue_WhenAllKeysPresent()
        {
            var inv = new KeyInventory();
            inv.AddKey("poststelle");
            inv.AddKey("deko");
            Assert.IsTrue(inv.HasKeys(new[] { "poststelle", "deko" }));
        }

        // ── K4: RemoveKeys entfernt Schlüssel ─────────────────────────────────────
        [Test]
        public void RemoveKeys_RemovesSpecifiedKeys()
        {
            var inv = new KeyInventory();
            inv.AddKey("poststelle");
            inv.AddKey("deko");
            inv.RemoveKeys(new[] { "poststelle" });
            Assert.IsFalse(inv.HasKeys(new[] { "poststelle" }));
            Assert.IsTrue(inv.HasKeys(new[] { "deko" }));
        }
    }
}
