using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceAbilityEngineTests
    {
        private static BoardState CreateAdjacentBoard(PieceDefinition source, string sourceId, PieceDefinition neighbor, string neighborId)
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(source, TestBoards.SupportLineAnchor(0), sourceId).Success);
            Assert.IsTrue(board.TryPlace(neighbor, TestBoards.SupportLineAnchor(1), neighborId).Success);
            return board;
        }

        private static PieceAbilityDefinition CreateMedicArmorAbility() => new()
        {
            Id = "medic_adjacent_infantry_armor_plus_one",
            Trigger = PieceAbilityTrigger.AdjacentAura,
            NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
            Stat = SynergyStat.ArmorType,
            ModType = SynergyModType.Flat,
            Magnitude = 1
        };

        private static PieceAbilityDefinition CreateCommandDamageAbility() => new()
        {
            Id = "command_adjacent_artillery_damage_plus_two",
            Trigger = PieceAbilityTrigger.AdjacentAura,
            NeighborFilter = new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery },
            Stat = SynergyStat.Damage,
            ModType = SynergyModType.Flat,
            Magnitude = 2
        };

        private static PieceAbilityDefinition CreateInspiringMoveAbility() => new()
        {
            Id = "inspiring_adjacent_move_charge_plus_five",
            Trigger = PieceAbilityTrigger.AdjacentAura,
            NeighborFilter = NeighborFilter.Any,
            Stat = SynergyStat.MoveChargePercent,
            ModType = SynergyModType.Flat,
            Magnitude = 5
        };

        [Test]
        public void PieceWithNoAbilities_ProducesZeroBonuses()
        {
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("rifle_1", out var result));
            Assert.AreEqual(0, result.DamageBonus);
            Assert.AreEqual(0, result.ArmorBuffSteps);
            Assert.AreEqual(0, result.MoveChargeBonus);
        }

        [Test]
        public void MedicTagAlone_DoesNotGrantArmorBuff()
        {
            var medic = TestPieces.CreateUnit(
                "medic",
                systemTag: GameTagIds.Combatant,
                synergyTags: new[] { GameTagIds.Medic });
            var infantry = TestPieces.CreateUnit(
                "infantry",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var board = CreateAdjacentBoard(medic, "medic_1", infantry, "infantry_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("infantry_1", out var result));
            Assert.AreEqual(0, result.ArmorBuffSteps);
        }

        [Test]
        public void MedicArmorAbility_AdjacentInfantry_GrantsArmorBuff()
        {
            var medic = TestPieces.With(
                TestPieces.CreateUnit("medic", systemTag: GameTagIds.Combatant),
                abilities: new[] { CreateMedicArmorAbility() });
            var infantry = TestPieces.CreateUnit(
                "infantry",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var board = CreateAdjacentBoard(medic, "medic_1", infantry, "infantry_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("infantry_1", out var result));
            Assert.AreEqual(1, result.ArmorBuffSteps);
        }

        [Test]
        public void CommandDamageAbility_AdjacentArtillery_GrantsDamageBonus()
        {
            var command = TestPieces.With(
                TestPieces.CreateUnit("command", systemTag: GameTagIds.Combatant),
                abilities: new[] { CreateCommandDamageAbility() });
            var artillery = TestPieces.CreateUnit(
                "artillery",
                combatRole: GameTagIds.Artillery,
                systemTag: GameTagIds.Combatant);
            var board = CreateAdjacentBoard(command, "command_1", artillery, "artillery_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("artillery_1", out var result));
            Assert.AreEqual(2, result.DamageBonus);
        }

        [Test]
        public void MoveChargeAbility_AdjacentAny_GrantsMoveCharge()
        {
            var inspiring = TestPieces.With(
                TestPieces.CreateUnit("inspiring", systemTag: GameTagIds.Combatant),
                abilities: new[] { CreateInspiringMoveAbility() });
            var neighbor = TestPieces.CreateUnit(
                "neighbor",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var board = CreateAdjacentBoard(inspiring, "inspiring_1", neighbor, "neighbor_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("neighbor_1", out var result));
            Assert.AreEqual(5, result.MoveChargeBonus);
        }

        [Test]
        public void FightStartSnapshot_DoesNotChangeAfterRelocate()
        {
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);

            var initialSnapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsFalse(initialSnapshot.TryGet("rifle_1", out var initialRifleResult) && initialRifleResult.DamageBonus > 0);

            var moved = board.TryRelocate("rifle_1", TestBoards.FrontLineAnchor(0), PieceRotation.R0);
            Assert.IsTrue(moved.Success, moved.Reason);

            var movedSnapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsFalse(movedSnapshot.TryGet("rifle_1", out var movedRifleResult) && movedRifleResult.DamageBonus > 0);
        }
    }
}
