using CozySanta.Runtime.Areas;
using NUnit.Framework;
using UnityEngine;

namespace CozySanta.Tests.EditMode
{
    /// <summary>EditMode-Tests für AreaManager (Z1–Z2).</summary>
    public sealed class AreaManagerTests
    {
        private AreaManager _mgr;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("AreaManager");
            _mgr = go.AddComponent<AreaManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mgr.gameObject);
        }

        private static AreaZone MakeZone(string name)
        {
            var go  = new GameObject(name);
            go.AddComponent<BoxCollider>();
            return go.AddComponent<AreaZone>();
        }

        // ── Z1: Zwei registrierte Zones → beide aktiv ─────────────────────────────
        [Test]
        public void RegisterZone_TwoZones_BothActive()
        {
            var z1 = MakeZone("ZoneA");
            var z2 = MakeZone("ZoneB");

            _mgr.RegisterZone(z1);
            _mgr.RegisterZone(z2);

            Assert.AreEqual(2, _mgr.ActiveZones.Count);

            Object.DestroyImmediate(z1.gameObject);
            Object.DestroyImmediate(z2.gameObject);
        }

        // ── Z2: Nach UnregisterZone → nur noch eine Zone aktiv ───────────────────
        [Test]
        public void UnregisterZone_RemovesZone_OtherRemains()
        {
            var z1 = MakeZone("ZoneA");
            var z2 = MakeZone("ZoneB");

            _mgr.RegisterZone(z1);
            _mgr.RegisterZone(z2);
            _mgr.UnregisterZone(z1);

            Assert.AreEqual(1, _mgr.ActiveZones.Count);
            Assert.IsTrue(System.Linq.Enumerable.Contains(_mgr.ActiveZones, z2));

            Object.DestroyImmediate(z1.gameObject);
            Object.DestroyImmediate(z2.gameObject);
        }
    }
}
