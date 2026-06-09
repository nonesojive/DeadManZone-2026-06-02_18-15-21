using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceCardViewModelBuilderTests
    {
        [Test]
        public void Build_IncludesStatIconsAndIdentityChipsOnly()
        {
            var piece = new PieceDefinition
            {
                Id = "flamethrower_trooper",
                DisplayName = "Flamethrower Trooper",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                FactionId = "neutral",
                MaxHp = 10,
                BaseDamage = 4,
                MovementSpeed = MovementSpeedTier.Medium,
                AttackSpeed = AttackSpeedTier.Slow,
                AttackType = AttackType.Ballistic,
                ArmorType = ArmorType.Light,
                AbilityTags = new[] { "flamethrower" },
                SynergyTags = System.Array.Empty<string>()
            };

            PieceCardViewModel model = PieceCardViewModelBuilder.Build(piece);

            Assert.AreEqual("Flamethrower Trooper", model.DisplayName);
            Assert.AreEqual(10, model.Hp);
            Assert.AreEqual(4, model.BaseDamage);
            Assert.AreEqual(MovementSpeedTier.Medium, model.MovementSpeed);
            Assert.AreEqual(AttackSpeedTier.Slow, model.AttackSpeed);
            Assert.AreEqual(AttackType.Ballistic, model.AttackType);
            Assert.AreEqual(ArmorType.Light, model.ArmorType);

            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Infantry));
            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Assault));
            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == "neutral"));
            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Ballistic));
            Assert.IsFalse(model.IdentityTags.Any(t => t.Id == GameTagIds.Combatant));

            Assert.AreEqual(1, model.OptionalTags.Count);
            Assert.AreEqual(0, model.OverflowCount);
            Assert.IsTrue(model.OptionalTags.Any(t => t.Id == "flamethrower"));
        }
    }
}
