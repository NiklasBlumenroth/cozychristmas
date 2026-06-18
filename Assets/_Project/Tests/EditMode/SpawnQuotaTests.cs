using System.Collections.Generic;
using CozySanta.Core.Items;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class SpawnQuotaTests
    {
        private static readonly List<string> Keys = new List<string> { "A", "B", "C" };

        // SQ1: Volle Varianten werden übersprungen, nur die freie wird gewählt.
        [Test]
        public void TryPick_SkipsFullVariants()
        {
            var counts = new Dictionary<string, int> { { "A", 20 }, { "B", 20 }, { "C", 5 } };
            // random01 egal: nur „C" ist spawnbar.
            Assert.IsTrue(SpawnQuota.TryPick(Keys, counts, 20, 0.0, out var key));
            Assert.AreEqual("C", key);
            Assert.IsTrue(SpawnQuota.TryPick(Keys, counts, 20, 0.99, out key));
            Assert.AreEqual("C", key);
        }

        // SQ2: random01 wählt den Index in der Liste der spawnbaren Varianten.
        [Test]
        public void TryPick_IndexesSpawnableByRandom()
        {
            var counts = new Dictionary<string, int>(); // alle 0 -> alle spawnbar
            SpawnQuota.TryPick(Keys, counts, 20, 0.0, out var first);
            SpawnQuota.TryPick(Keys, counts, 20, 0.999, out var last);
            Assert.AreEqual("A", first);
            Assert.AreEqual("C", last);
        }

        // SQ3: Alles voll -> kein Spawn, IsFull true.
        [Test]
        public void AllAtMax_IsFull_AndNoPick()
        {
            var counts = new Dictionary<string, int> { { "A", 20 }, { "B", 20 }, { "C", 20 } };
            Assert.IsTrue(SpawnQuota.IsFull(Keys, counts, 20));
            Assert.IsFalse(SpawnQuota.TryPick(Keys, counts, 20, 0.5, out _));
        }

        // SQ4: Fehlende Einträge zählen als 0 (noch spawnbar).
        [Test]
        public void MissingCount_TreatedAsZero()
        {
            var counts = new Dictionary<string, int> { { "A", 20 } };
            Assert.IsFalse(SpawnQuota.IsFull(Keys, counts, 20));
            Assert.IsTrue(SpawnQuota.TryPick(Keys, counts, 20, 0.0, out var key));
            Assert.AreEqual("B", key); // A ist voll -> erste spawnbare ist B
        }

        // SQ5: Variantenspezifische Höchstzahl – „A" hat ein höheres Limit und bleibt spawnbar,
        // obwohl es über dem Default liegt; „B"/„C" sind beim Default voll.
        [Test]
        public void PerKeyMax_OverridesDefault()
        {
            var counts = new Dictionary<string, int> { { "A", 30 }, { "B", 20 }, { "C", 20 } };
            var maxByKey = new Dictionary<string, int> { { "A", 75 } }; // A: 75, Rest: Default 20
            Assert.IsFalse(SpawnQuota.IsFull(Keys, counts, maxByKey, 20));
            Assert.IsTrue(SpawnQuota.TryPick(Keys, counts, maxByKey, 20, 0.0, out var key));
            Assert.AreEqual("A", key); // nur A ist noch unter seinem (höheren) Limit
        }

        // SQ6: Mit Overrides alle voll -> IsFull true, kein Pick (A bei 75, B/C bei Default 20).
        [Test]
        public void PerKeyMax_AllFull()
        {
            var counts = new Dictionary<string, int> { { "A", 75 }, { "B", 20 }, { "C", 20 } };
            var maxByKey = new Dictionary<string, int> { { "A", 75 } };
            Assert.IsTrue(SpawnQuota.IsFull(Keys, counts, maxByKey, 20));
            Assert.IsFalse(SpawnQuota.TryPick(Keys, counts, maxByKey, 20, 0.5, out _));
        }

        // SQ7: Override 0 (oder fehlend) fällt auf den Default zurück.
        [Test]
        public void PerKeyMax_ZeroFallsBackToDefault()
        {
            var counts = new Dictionary<string, int> { { "A", 20 } };
            var maxByKey = new Dictionary<string, int> { { "A", 0 } }; // 0 = kein Override -> Default 20
            Assert.IsTrue(SpawnQuota.IsFull(new List<string> { "A" }, counts, maxByKey, 20));
        }
    }
}
