using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceTagQueriesTests
    {
        [Test]
        public void HasTag_MatchesPrimaryAndSynergy()
        {
            var piece = new PieceDefinition
            {
                Id = "test",
                DisplayName = "Test",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                SynergyTags = new[] { GameTagIds.Vanguard }
            };

            Assert.IsTrue(PieceTagQueries.HasTag(piece, GameTagIds.Infantry));
            Assert.IsTrue(PieceTagQueries.HasTag(piece, GameTagIds.Vanguard));
            Assert.IsFalse(PieceTagQueries.HasTag(piece, GameTagIds.Vehicle));
        }

        [Test]
        public void GetPlayerVisibleTags_ExcludesSystemTags()
        {
            var piece = new PieceDefinition
            {
                Id = "test",
                DisplayName = "Test",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                FactionId = "neutral",
                SynergyTags = new[] { "a", "b", "c", "d", "e" },
                AbilityTags = new[] { "flamethrower" }
            };

            var result = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            Assert.IsFalse(result.VisibleTags.Any(t => t.Id == GameTagIds.Combatant));
            Assert.LessOrEqual(result.OptionalTags.Count, 4);
            Assert.Greater(result.OverflowCount, 0);
        }
    }
}
