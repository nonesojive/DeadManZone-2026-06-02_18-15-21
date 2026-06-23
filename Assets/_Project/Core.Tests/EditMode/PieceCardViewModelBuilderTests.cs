using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceCardViewModelBuilderTests
    {
        private static PieceAbilityDefinition CreateMedicArmorAbility() => new()
        {
            Id = "medic_adjacent_infantry_armor_plus_one",
            Trigger = PieceAbilityTrigger.AdjacentAura,
            NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
            Stat = SynergyStat.ArmorType,
            ModType = SynergyModType.Flat,
            Magnitude = 1
        };

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
            Assert.AreEqual(5, model.MovementSpeedValue);
            Assert.AreEqual(AttackSpeedTier.Slow, model.AttackSpeed);
            Assert.AreEqual(AttackType.Ballistic, model.AttackType);
            Assert.AreEqual(ArmorType.Light, model.ArmorType);

            Assert.AreEqual(GameTagIds.Infantry, model.PrimaryTag.Id);
            Assert.AreEqual(GameTagIds.Assault, model.CombatRoleTag.Id);

            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Infantry));
            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Assault));
            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == "neutral"));
            Assert.IsTrue(model.IdentityTags.Any(t => t.Id == GameTagIds.Ballistic));
            Assert.IsFalse(model.IdentityTags.Any(t => t.Id == GameTagIds.Combatant));

            Assert.IsFalse(model.ChipTags.Any(t => t.Id == GameTagIds.Infantry));
            Assert.IsFalse(model.ChipTags.Any(t => t.Id == GameTagIds.Assault));
            Assert.IsFalse(model.ChipTags.Any(t => t.Id == GameTagIds.Ballistic));
            Assert.IsTrue(model.ChipTags.Any(t => t.Id == "neutral"));

            Assert.AreEqual(1, model.OptionalTags.Count);
            Assert.AreEqual(1, model.ChipTags.Count(t => t.Id == "flamethrower"));
            Assert.AreEqual(0, model.OverflowCount);
        }

        [Test]
        public void Build_IncludesAttackAndArmorTooltips()
        {
            var piece = new PieceDefinition
            {
                Id = "fire_unit",
                DisplayName = "Fire Unit",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                AttackType = AttackType.Fire,
                ArmorType = ArmorType.Heavy
            };

            PieceCardViewModel model = PieceCardViewModelBuilder.Build(piece);

            StringAssert.Contains("Light armor", model.AttackTypeTooltip);
            StringAssert.Contains("Heavy plating", model.ArmorTypeTooltip);
        }

        [Test]
        public void Build_WithSalvagedContext_IncludesSalvageLine()
        {
            var piece = TestPieces.RifleSquad();
            var context = new PieceCardBuildContext
            {
                IsSalvaged = true,
                LastEnemyFactionId = "FactionIds.DustScourge",
                LastEnemyFactionDisplayName = "Dust Scourge"
            };

            PieceCardViewModel model = PieceCardViewModelBuilder.Build(piece, context);

            Assert.AreEqual("Salvaged from Dust Scourge", model.SalvageContext);
        }

        [Test]
        public void Build_WithBoardAndMedicSynergy_IncludesSynergyLinesAndBonus()
        {
            var medic = TestPieces.CreateUnit(
                "Field Medic",
                systemTag: GameTagIds.Combatant,
                synergyTags: new[] { GameTagIds.Medic });
            medic = TestPieces.With(medic, abilities: new[] { CreateMedicArmorAbility() });
            var infantry = TestPieces.CreateUnit(
                "Rifle Squad",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);

            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(medic, TestBoards.SupportLineAnchor(0), "medic_1").Success);
            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(1), "infantry_1").Success);

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("infantry_1", out var synergy));

            var context = new PieceCardBuildContext
            {
                Board = board,
                InstanceId = "infantry_1",
                Synergy = synergy,
                SynergySnapshot = snapshot
            };

            PieceCardViewModel model = PieceCardViewModelBuilder.Build(infantry, context);

            Assert.AreEqual(1, model.SynergyArmorBuffSteps);
            Assert.AreEqual(1, model.SynergyLines.Count);
            StringAssert.Contains("Field Medic", model.SynergyLines[0]);
            StringAssert.Contains("+1 Armor", model.SynergyLines[0]);
        }

        [Test]
        public void Build_WithGrantedAbility_IncludesAbilityText()
        {
            var piece = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.GrenadeLob);
            PieceCardViewModel model = PieceCardViewModelBuilder.Build(piece);

            StringAssert.Contains("Grenade Lob", model.AbilityText);
        }

        [Test]
        public void Build_WithCatalogAbility_IncludesAbilityLine()
        {
            var aura = new PieceAbilityDefinition
            {
                Id = "adjacent_allies_move_plus_one",
                CardDescription = "Adjacent allies gain +1 move step."
            };
            var piece = TestPieces.With(TestPieces.RifleSquad(), abilities: new[] { aura });
            var vm = PieceCardViewModelBuilder.Build(piece);

            Assert.That(vm.AbilityLines, Does.Contain("Adjacent allies gain +1 move step."));
        }
    }
}
