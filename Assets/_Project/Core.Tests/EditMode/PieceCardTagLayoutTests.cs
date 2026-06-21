using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceCardTagLayoutTests
    {
        [Test]
        public void BuildChipTags_ExcludesPrimaryCombatRoleAndAttackType()
        {
            var piece = new PieceDefinition
            {
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                AttackType = AttackType.Ballistic,
                FactionId = "neutral",
                AbilityTags = new[] { "flamethrower" }
            };

            var visible = PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);
            var chips = PieceCardTagLayout.BuildChipTags(visible);

            Assert.AreEqual(GameTagIds.Infantry, PieceCardTagLayout.ResolvePrimaryTag(visible).Id);
            Assert.AreEqual(GameTagIds.Assault, PieceCardTagLayout.ResolveCombatRoleTag(visible).Id);
            Assert.IsTrue(chips.Any(t => t.Id == "neutral"));
            Assert.IsTrue(chips.Any(t => t.Id == "flamethrower"));
            Assert.IsFalse(chips.Any(t => t.Id == GameTagIds.Infantry));
            Assert.IsFalse(chips.Any(t => t.Id == GameTagIds.Assault));
            Assert.IsFalse(chips.Any(t => t.Id == GameTagIds.Ballistic));
        }
    }
}
