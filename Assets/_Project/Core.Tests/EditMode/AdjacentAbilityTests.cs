using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class AdjacentAbilityTests
    {
        private static BoardState PlaceAdjacent(PieceDefinition left, string leftId, PieceDefinition right, string rightId)
        {
            var board = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(board.TryPlace(left, TestBoards.CombatBoardAnchor(0, 0), leftId).Success);
            Assert.IsTrue(board.TryPlace(right, TestBoards.CombatBoardAnchor(1, 0), rightId).Success);
            return board;
        }

        [Test]
        public void FieldMedic_AdjacentInfantry_GrantsTenMaxHp()
        {
            var board = PlaceAdjacent(
                TestPieces.FieldMedic(),
                "medic_1",
                TestPieces.CreateUnit("infantry", primary: GameTagIds.Infantry),
                "infantry_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("infantry_1", out var result));
            Assert.AreEqual(10, result.MaxHpFlat);
        }

        [Test]
        public void BulwarkSquad_AdjacentPhalanx_GrantsDamageAndHpToSelf()
        {
            var phalanxNeighbor = TestPieces.CreateUnit(
                "phalanx_neighbor",
                primary: GameTagIds.Infantry,
                synergyTags: new[] { GameTagIds.Phalanx });
            var board = PlaceAdjacent(TestPieces.BulwarkSquad(), "bulwark_1", phalanxNeighbor, "phalanx_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("bulwark_1", out var result));
            Assert.AreEqual(1, result.DamageBonus);
            Assert.AreEqual(5, result.MaxHpFlat);
        }

        [Test]
        public void BulwarkSquad_Alone_NoSelfBonus()
        {
            var board = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(board.TryPlace(TestPieces.BulwarkSquad(), TestBoards.CombatBoardAnchor(0, 0), "bulwark_1").Success);

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("bulwark_1", out var result));
            Assert.AreEqual(0, result.DamageBonus);
            Assert.AreEqual(0, result.MaxHpFlat);
        }

        [Test]
        public void EnlistedRifleman_AdjacentCommand_GrantsAttackSpeedStep()
        {
            var board = PlaceAdjacent(
                TestPieces.EnlistedRifleman(),
                "rifleman_1",
                TestPieces.WithTags(command: true),
                "command_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("rifleman_1", out var result));
            Assert.AreEqual(1, result.AttackSpeedSteps);
        }

        [Test]
        public void IronHorse_AdjacentInfantry_GrantsTenHpPerNeighborToSelf()
        {
            var board = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(board.TryPlace(TestPieces.IronmarchIronHorse(), TestBoards.CombatBoardAnchor(0, 0), "horse_1").Success);
            Assert.IsTrue(board.TryPlace(
                TestPieces.CreateUnit("infantry_a", primary: GameTagIds.Infantry),
                TestBoards.CombatBoardAnchor(1, 0),
                "infantry_a").Success);
            Assert.IsTrue(board.TryPlace(
                TestPieces.CreateUnit("infantry_b", primary: GameTagIds.Infantry),
                TestBoards.CombatBoardAnchor(0, 1),
                "infantry_b").Success);

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("horse_1", out var result));
            Assert.AreEqual(20, result.MaxHpFlat);
        }

        [Test]
        public void FieldMarshal_AdjacentInfantry_GrantsHpAndMovement()
        {
            var board = PlaceAdjacent(
                TestPieces.IroncladFieldMarshal(),
                "marshal_1",
                TestPieces.CreateUnit("infantry", primary: GameTagIds.Infantry),
                "infantry_1");

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("infantry_1", out var result));
            Assert.AreEqual(5, result.MaxHpFlat);
            Assert.AreEqual(1, result.MovementSpeedBonus);
        }

        [Test]
        public void ApplyToCombatants_AppliesMovementSpeedBonus()
        {
            var infantry = TestPieces.CreateUnit("infantry", primary: GameTagIds.Infantry);
            var board = PlaceAdjacent(TestPieces.IroncladFieldMarshal(), "marshal_1", infantry, "infantry_1");
            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);

            var combatants = new List<CombatantState>
            {
                new()
                {
                    InstanceId = "infantry_1",
                    Definition = infantry,
                    CurrentHp = infantry.MaxHp
                }
            };

            PieceAbilityEngine.ApplyToCombatants(snapshot, combatants);
            Assert.AreEqual(1, combatants[0].MovementSpeedBonus);
            Assert.AreEqual(2 + 1, combatants[0].EffectiveMovementSpeed);
        }

        [Test]
        public void ApplyToCombatants_AppliesAttackSpeedSteps()
        {
            var rifleman = TestPieces.EnlistedRifleman();
            var board = PlaceAdjacent(rifleman, "rifleman_1", TestPieces.WithTags(command: true), "command_1");
            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);

            var combatants = new List<CombatantState>
            {
                new()
                {
                    InstanceId = "rifleman_1",
                    Definition = rifleman,
                    CurrentHp = rifleman.MaxHp
                }
            };

            PieceAbilityEngine.ApplyToCombatants(snapshot, combatants);
            Assert.AreEqual(1, combatants[0].AttackSpeedSteps);
        }

        // ---------------------------------------------------------------
        // 2026-07-15 faction-roster-v1 §2.2: Breakthrough Tank's "infantry within 2 cells
        // gain morale resistance" — an AdjacentAura at Radius 2, reusing board-adjacency
        // topology at 2 hops rather than raw grid-distance geometry.
        // ---------------------------------------------------------------

        private static PieceDefinition TankWithMoraleResistAura(int radius) => TestPieces.With(
            TestPieces.CreateUnit("breakthrough_tank", primary: GameTagIds.Vehicle, combatRole: GameTagIds.Tank),
            abilities: new[]
            {
                new PieceAbilityDefinition
                {
                    Id = "breakthrough_tank_infantry_morale_resist",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                    Stat = SynergyStat.MoraleResistancePercent,
                    ModType = SynergyModType.Percent,
                    Magnitude = 25,
                    Radius = radius
                }
            });

        [Test]
        public void BreakthroughTank_Radius2Aura_ReachesTwoHopNeighbor()
        {
            var board = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(board.TryPlace(TankWithMoraleResistAura(radius: 2), TestBoards.CombatBoardAnchor(0, 0), "tank_1").Success);
            Assert.IsTrue(board.TryPlace(TestPieces.CreateUnit("infantry_a", primary: GameTagIds.Infantry), TestBoards.CombatBoardAnchor(1, 0), "infantry_a").Success);
            Assert.IsTrue(board.TryPlace(TestPieces.CreateUnit("infantry_b", primary: GameTagIds.Infantry), TestBoards.CombatBoardAnchor(2, 0), "infantry_b").Success);

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);

            Assert.IsTrue(snapshot.TryGet("infantry_a", out var oneHop));
            Assert.AreEqual(25, oneHop.MoraleResistancePercent);

            Assert.IsTrue(snapshot.TryGet("infantry_b", out var twoHops));
            Assert.AreEqual(25, twoHops.MoraleResistancePercent, "within 2 board-adjacency hops must be reached");
        }

        [Test]
        public void BreakthroughTank_Radius2Aura_DoesNotReachThreeHopNeighbor()
        {
            var board = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(board.TryPlace(TankWithMoraleResistAura(radius: 2), TestBoards.CombatBoardAnchor(0, 0), "tank_1").Success);
            Assert.IsTrue(board.TryPlace(TestPieces.CreateUnit("infantry_a", primary: GameTagIds.Infantry), TestBoards.CombatBoardAnchor(1, 0), "infantry_a").Success);
            Assert.IsTrue(board.TryPlace(TestPieces.CreateUnit("infantry_b", primary: GameTagIds.Infantry), TestBoards.CombatBoardAnchor(2, 0), "infantry_b").Success);
            Assert.IsTrue(board.TryPlace(TestPieces.CreateUnit("infantry_c", primary: GameTagIds.Infantry), TestBoards.CombatBoardAnchor(3, 0), "infantry_c").Success);

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);

            Assert.IsTrue(snapshot.TryGet("infantry_c", out var threeHops));
            Assert.AreEqual(0, threeHops.MoraleResistancePercent, "3 hops away is outside the radius-2 aura");
        }

        [Test]
        public void ApplyToCombatants_AppliesMoraleResistancePercent()
        {
            var infantry = TestPieces.CreateUnit("infantry", primary: GameTagIds.Infantry);
            var board = PlaceAdjacent(TankWithMoraleResistAura(radius: 2), "tank_1", infantry, "infantry_1");
            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);

            var combatants = new List<CombatantState>
            {
                new() { InstanceId = "infantry_1", Definition = infantry, CurrentHp = infantry.MaxHp }
            };

            PieceAbilityEngine.ApplyToCombatants(snapshot, combatants);
            Assert.AreEqual(25, combatants[0].MoraleDamageResistancePercent);
        }
    }
}
