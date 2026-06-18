using CozySanta.Core.Sorting;
using NUnit.Framework;

namespace CozySanta.Tests.EditMode
{
    public sealed class SortPlacementRuleTests
    {
        // SP1: Keine/leere Erlaubnisliste -> jedes Item einlegbar (Standardverhalten bleibt).
        [Test]
        public void NoAllowedArts_AlwaysPlaceable()
        {
            var key = new SortKey("Keks", "Braun_Blau");
            Assert.IsTrue(SortPlacementRule.IsPlaceable(key, null));
            Assert.IsTrue(SortPlacementRule.IsPlaceable(key, new string[0]));
        }

        // SP2: Passende Art (erste Facette) ist erlaubt.
        [Test]
        public void MatchingArt_Placeable()
        {
            var shelf = new[] { "Zuckerstange", "Lebkuchen" };
            Assert.IsTrue(SortPlacementRule.IsPlaceable(new SortKey("Zuckerstange", "Rot"), shelf));
            Assert.IsTrue(SortPlacementRule.IsPlaceable(new SortKey("Lebkuchen", "Red"), shelf));
        }

        // SP3: Fremde Art ist gesperrt (Keks gehört nicht ins Regal, Zuckerstange nicht in die Crate).
        [Test]
        public void ForeignArt_Blocked()
        {
            Assert.IsFalse(SortPlacementRule.IsPlaceable(new SortKey("Keks", "Braun_Blau"),
                new[] { "Zuckerstange", "Lebkuchen" }));
            Assert.IsFalse(SortPlacementRule.IsPlaceable(new SortKey("Zuckerstange", "Rot"),
                new[] { "Keks" }));
        }

        // SP4: Item ohne Art (leerer Key) ist bei gesetzter Liste nicht erlaubt.
        [Test]
        public void EmptyKey_BlockedWhenRestricted()
        {
            Assert.IsFalse(SortPlacementRule.IsPlaceable(default, new[] { "Keks" }));
            Assert.IsTrue(SortPlacementRule.IsPlaceable(default, null)); // ohne Liste weiterhin erlaubt
        }
    }
}
