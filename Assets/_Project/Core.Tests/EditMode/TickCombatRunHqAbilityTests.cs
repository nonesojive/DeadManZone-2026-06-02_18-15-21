using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §4 (🟡 ledger, now wired): HQ-board buildings can
    /// grant pause-window abilities (Artillery Park's Ranging Barrage), and Grand Battery's
    /// Rolling Barrage scales with the army's artillery-tag count across both boards.</summary>
    public sealed class TickCombatRunHqAbilityTests
    {
        [Test]
        public void HqBuilding_CanFireGrantedAbility_AtOpeningPause()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "player_rifle");

            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var artilleryPark = TestPieces.With(TestPieces.CommandOutpost(), grantedAbility: GrantedAbility.MortarShot);
            hq.TryPlace(artilleryPark, new GridCoord(0, 0), "artillery_park_1");

            var enemy = TestBoards.WeakEnemyOnly();
            var buildBoards = new BuildBoardSet { Combat = player, Hq = hq };

            var run = TickCombatRun.Start(player, enemy, seed: 7, authority: 2, playerBuildBoards: buildBoards);
            var weakAnchor = run.EnemyCombatantsForTests.Single().AnchorPosition;

            var result = run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "artillery_park_1",
                    TargetCell = weakAnchor
                }
            });

            Assert.AreEqual(CombatAdvanceStatus.Completed, result.Status,
                "the HQ-sourced MortarShot must one-shot the weak conscript, same as a combat-board source would");
            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "mortar_shot" && e.ActorId == "artillery_park_1"));
        }

        [Test]
        public void HqBuilding_AbilityRejected_WhenSourceIdUnknown()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "player_rifle");
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var enemy = TestBoards.WeakEnemyOnly();
            var buildBoards = new BuildBoardSet { Combat = player, Hq = hq };

            var run = TickCombatRun.Start(player, enemy, seed: 7, authority: 2, playerBuildBoards: buildBoards);
            var weakAnchor = run.EnemyCombatantsForTests.Single().AnchorPosition;

            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "not_a_real_piece",
                    TargetCell = weakAnchor
                }
            });

            // The bogus command must be rejected outright — no mortar_shot event ever
            // attributed to a source that exists on neither the combat nor the HQ board.
            Assert.IsFalse(run.Log.Events.Any(e => e.ActorId == "not_a_real_piece"));
        }

        [Test]
        public void RollingBarrage_ScalesWithArtilleryCountAcrossBothBoards()
        {
            var grandBattery = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.RollingBarrage);

            var lowArtilleryPlayer = new BoardState(TestBoards.Layout);
            lowArtilleryPlayer.TryPlace(grandBattery, TestBoards.FrontLineAnchor(3), "battery_1");
            var lowArtilleryHq = new BoardState(TestBoards.IronMarchHqLayout);
            var lowRun = TickCombatRun.Start(
                lowArtilleryPlayer,
                TestBoards.StandardEnemyRiflesOnly(),
                seed: 5,
                authority: 3,
                playerBuildBoards: new BuildBoardSet { Combat = lowArtilleryPlayer, Hq = lowArtilleryHq });
            var lowTargetAnchor = lowRun.EnemyCombatantsForTests.First().AnchorPosition;
            lowRun.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.RollingBarrage,
                    SourcePieceId = "battery_1",
                    TargetCell = lowTargetAnchor
                }
            });
            int lowDamage = lowRun.Log.Events
                .Where(e => e.ActionType == "rolling_barrage")
                .Sum(e => e.Value);

            var artillery = new PieceDefinition
            {
                Id = "field_mortar_team",
                DisplayName = "Field Mortar Team",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Artillery,
                MaxHp = 30,
                ManpowerCost = 1
            };

            var highArtilleryPlayer = new BoardState(TestBoards.Layout);
            highArtilleryPlayer.TryPlace(grandBattery, TestBoards.FrontLineAnchor(3), "battery_1");
            highArtilleryPlayer.TryPlace(artillery, TestBoards.FrontLineAnchor(4), "mortar_1");
            highArtilleryPlayer.TryPlace(artillery, TestBoards.FrontLineAnchor(5), "mortar_2");
            var highArtilleryHq = new BoardState(TestBoards.IronMarchHqLayout);
            var highRun = TickCombatRun.Start(
                highArtilleryPlayer,
                TestBoards.StandardEnemyRiflesOnly(),
                seed: 5,
                authority: 3,
                playerBuildBoards: new BuildBoardSet { Combat = highArtilleryPlayer, Hq = highArtilleryHq });
            var highTargetAnchor = highRun.EnemyCombatantsForTests.First().AnchorPosition;
            highRun.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.RollingBarrage,
                    SourcePieceId = "battery_1",
                    TargetCell = highTargetAnchor
                }
            });
            int highDamage = highRun.Log.Events
                .Where(e => e.ActionType == "rolling_barrage")
                .Sum(e => e.Value);

            Assert.Greater(highDamage, lowDamage,
                "2 extra artillery-tagged pieces on the board must raise Rolling Barrage's damage");
        }
    }
}
