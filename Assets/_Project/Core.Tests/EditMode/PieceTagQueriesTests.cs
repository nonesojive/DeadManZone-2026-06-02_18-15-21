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
                AttackType = AttackType.Piercing,
                FactionId = "neutral"
            };

            var result = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            Assert.IsTrue(result.IdentityTags.Any(t => t.Id == GameTagIds.Piercing));
        }

        [Test]
        public void GetPlayerVisibleTags_IncludesRegisteredKeywordTags()
        {
            var piece = new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
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
        public void GetPlayerVisibleTags_FiltersObsoleteLegacyCombatantDisplayNames()
        {
            var piece = new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                Tags = new[] { "Combatant", "Infantry", "Non-Combatant" }
            };

            var result = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            Assert.IsFalse(result.VisibleTags.Any(t => t.DisplayName == "Combatant"));
            Assert.IsFalse(result.VisibleTags.Any(t => t.DisplayName == "Non-Combatant"));
        }

        [Test]
        public void BuildLegacyTags_OmitsObsoleteSystemTags()
        {
            var tags = PieceTagQueries.BuildLegacyTags(
                PieceCategory.Unit,
                baseDamage: 3,
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Assault,
                systemTag: "combatant",
                synergyTags: System.Array.Empty<string>(),
                abilityTags: System.Array.Empty<string>());

            Assert.IsFalse(tags.Any(t => t == "Combatant" || t == "combatant" || t == "HQ" || t == "hq"));
        }

        [Test]
        public void BuildLegacyTags_OmitsObsoleteHqSystemTag()
        {
            var tags = PieceTagQueries.BuildLegacyTags(
                PieceCategory.Building,
                baseDamage: 0,
                primary: GameTagIds.Building,
                combatRole: GameTagIds.Utility,
                systemTag: "hq",
                synergyTags: System.Array.Empty<string>(),
                abilityTags: System.Array.Empty<string>());

            Assert.IsFalse(tags.Any(t => t == "HQ" || t == "hq"));
        }
    }
}
