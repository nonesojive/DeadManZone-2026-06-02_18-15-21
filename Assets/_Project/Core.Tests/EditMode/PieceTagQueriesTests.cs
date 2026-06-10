using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceTagQueriesTests
    {
        [Test]
        public void HasTag_ReturnsTrueForPrimaryCombatRoleAndSynergyTags()
        {
            var piece = new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                SynergyTags = new[] { "future_trait" }
            };

            Assert.IsTrue(PieceTagQueries.HasTag(piece, GameTagIds.Infantry));
            Assert.IsTrue(PieceTagQueries.HasTag(piece, "future_trait"));
            Assert.IsFalse(PieceTagQueries.HasTag(piece, GameTagIds.Vehicle));
        }

        [Test]
        public void GetPlayerVisibleTags_IncludesAttackTypeChip()
        {
            var piece = new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                AttackType = AttackType.Piercing,
                FactionId = "neutral"
            };

            var result = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            Assert.IsTrue(result.IdentityTags.Any(t => t.Id == GameTagIds.Piercing));
            Assert.IsFalse(result.IdentityTags.Any(t => t.Id == GameTagIds.Combatant));
        }

        [Test]
        public void GetPlayerVisibleTags_IncludesRegisteredKeywordTags()
        {
            var piece = new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                SynergyTags = new[] { GameTagIds.Medic },
                AbilityTags = new[] { GameTagIds.Stealth },
                FlavorTags = new[] { GameTagIds.Veteran }
            };

            var result = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            Assert.IsTrue(result.OptionalTags.Any(t => t.Id == GameTagIds.Stealth));
            Assert.IsTrue(result.OptionalTags.Any(t => t.Id == GameTagIds.Veteran));
            Assert.IsTrue(result.OptionalTags.Any(t => t.Id == GameTagIds.Medic));
        }

        [Test]
        public void GetPlayerVisibleTags_HidesSystemTags()
        {
            var piece = new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant
            };

            var result = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            Assert.IsFalse(result.VisibleTags.Any(t => t.Id == GameTagIds.Combatant));
        }
    }
}
