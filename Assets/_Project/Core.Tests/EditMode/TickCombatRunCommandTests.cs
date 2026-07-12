using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>Pause-command regressions: event segment alignment (F1), occupancy release
    /// on ability kills (F2), pause-granted armor buff lifetime (F6), and ProtectSupport
    /// grant idempotence (M9).</summary>
    public sealed class TickCombatRunCommandTests
    {
        [Test]
        public void SetPlayerTactic_ProtectSupportTwice_GrantsRearArmorOnce()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(), "player_rifle");
            Assert.IsTrue(
                player.TryPlace(TestPieces.GasDrone(), new GridCoord(0, 0), "player_drone").Success,
                "hybrid drone must be placeable in the rear zone");

            var run = TickCombatRun.Start(player, TestBoards.WeakEnemyOnly(), seed: 9, authority: 2);
            var drone = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_drone");

            run.SetPlayerTactic(TacticType.ProtectSupport);
            Assert.AreEqual(TacticEffects.ProtectSupportRearArmorSteps, drone.ArmorBuffSteps,
                "first ProtectSupport application grants the rear armor");

            run.SetPlayerTactic(TacticType.ProtectSupport);
            Assert.AreEqual(TacticEffects.ProtectSupportRearArmorSteps, drone.ArmorBuffSteps,
                "re-applying ProtectSupport (ctor + restore path both call SetPlayerTactic) " +
                "must not double-grant permanent fight-start armor");
        }

        [Test]
        public void GetLiveEnemyTargetCells_MatchesExecutorTargetRule()
        {
            var run = TickCombatRun.Start(
                TestBoards.StandardPlayer(), TestBoards.StandardEnemy(), seed: 13, authority: 2);

            var cells = run.GetLiveEnemyTargetCells();
            var enemies = run.EnemyCombatantsForTests;

            Assert.IsNotEmpty(cells, "a fresh fight has live enemies to target");
            Assert.AreEqual(cells.Count, cells.Distinct().Count(), "no duplicate cells");

            // Both directions: every surfaced cell is honored by the executor, and every
            // cell the executor would honor is surfaced. Widening OccupiesCell (e.g. to
            // footprints) must update GetLiveEnemyTargetCells in the same change.
            foreach (var cell in cells)
                Assert.IsTrue(CombatAbilityExecutor.IsValidTargetCell(enemies, cell),
                    $"surfaced cell {cell} must satisfy the executor's target rule");

            var honoredAnchors = enemies
                .Where(e => e.IsAlive)
                .Select(e => e.AnchorPosition)
                .Distinct()
                .ToList();
            CollectionAssert.AreEquivalent(honoredAnchors, cells,
                "the pause UI must see exactly the cells the executor honors");
        }

        [Test]
        public void OpeningPauseAbilityKill_LogsKillAndFightEndInSameSegment()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot),
                TestBoards.FrontLineAnchor(3),
                "player_grenadier");
            var enemy = TestBoards.WeakEnemyOnly();

            var run = TickCombatRun.Start(player, enemy, seed: 7, authority: 2);
            var weakAnchor = run.EnemyCombatantsForTests.Single().AnchorPosition;

            var result = run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "player_grenadier",
                    TargetCell = weakAnchor
                }
            });

            Assert.AreEqual(CombatAdvanceStatus.Completed, result.Status);
            var destroyed = run.Log.Events.Single(e => e.ActionType == "destroyed");
            var fightEnd = run.Log.Events.Single(e => e.ActionType == "fight_end");
            Assert.AreEqual(result.SegmentIndex, destroyed.Segment,
                "pause-ability kill must land in the segment the playback plays");
            Assert.AreEqual(destroyed.Segment, fightEnd.Segment,
                "fight_end must share the kill's segment");
            Assert.LessOrEqual(run.Log.Events.Max(e => e.Segment), run.CheckpointsFired,
                "no event may exceed the max playable segment");
        }

        [Test]
        public void MidPauseAbilityKill_LogsInSegmentThatPlaysNext()
        {
            var run = StartMatchedFightWithRearConscript(authority: 3);

            var first = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status);
            Assert.AreEqual(1, run.CheckpointsFired);

            var weak = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_weak");
            Assert.IsTrue(weak.IsAlive, "rear conscript should survive to the mid pause");

            var second = run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 1,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "player_grenadier",
                    TargetCell = weak.AnchorPosition
                }
            });

            var destroyed = run.Log.Events.Single(e =>
                e.ActionType == "destroyed" && e.ActorId == "enemy_weak");
            Assert.AreEqual(1, destroyed.Segment,
                "mid-pause ability kill must land in segment 1 (the one playing next)");
            Assert.AreEqual(second.SegmentIndex, destroyed.Segment);
            Assert.LessOrEqual(run.Log.Events.Max(e => e.Segment), run.CheckpointsFired,
                "no event may exceed the max playable segment");
        }

        [Test]
        public void MidPauseAbilityKill_ReleasesOccupancyGrid()
        {
            var run = StartMatchedFightWithRearConscript(authority: 3);

            var first = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status);

            var weak = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_weak");
            Assert.IsTrue(weak.IsAlive);

            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 1,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "player_grenadier",
                    TargetCell = weak.AnchorPosition
                }
            });

            Assert.IsFalse(weak.IsAlive, "mortar shot should kill the 3hp conscript");
            Assert.IsFalse(
                run.OccupancySnapshotForTests.Values.Contains("enemy_weak"),
                "ability kill must free the victim's cells for pathfinding");
        }

        [Test]
        public void ShieldAllies_GrantedAtPause_SurvivesTheCommandBatch()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.ShieldAllies),
                TestBoards.FrontLineAnchor(4),
                "player_shield");
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(5), "player_rifle");
            var enemy = TestBoards.WeakEnemyOnly();

            var run = TickCombatRun.Start(player, enemy, seed: 11, authority: 2);

            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.ShieldAllies,
                    SourcePieceId = "player_shield"
                }
            });

            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "shield_allies" && e.TargetId == "player_rifle"),
                "ShieldAllies should log its grant to the adjacent infantry ally");
            var buffed = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_rifle");
            Assert.AreEqual(1, buffed.PauseArmorBuffSteps,
                "armor granted during the pause batch must survive until the next pause");
        }

        [Test]
        public void FightStartArmor_SurvivesCommandedPause()
        {
            var run = StartMatchedFightWithArmoredBuddy(authority: 3);
            var buddy = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_buddy");
            Assert.AreEqual(1, buddy.ArmorBuffSteps, "medic aura should grant fight-start armor");

            var first = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status);

            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 1,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "player_grenadier"
                }
            });

            Assert.AreEqual(1, buddy.ArmorBuffSteps,
                "fight-start armor is permanent — a commanded pause must not strip it (old F6 quirk)");
        }

        [Test]
        public void FightStartArmor_SurvivesWholeFight_WithCommandsSubmitted()
        {
            var run = StartMatchedFightWithArmoredBuddy(authority: 6);
            var buddy = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_buddy");

            var commands = new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "player_grenadier"
                },
                new PhaseCommand
                {
                    AfterCheckpoint = 1,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.MortarShot,
                    SourcePieceId = "player_grenadier"
                }
            };

            var result = run.Continue(commands);
            while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                result = run.Continue(commands);

            Assert.AreEqual(CombatAdvanceStatus.Completed, result.Status);
            Assert.AreEqual(1, buddy.ArmorBuffSteps,
                "fight-start armor must persist for the whole fight");
            Assert.AreEqual(0, buddy.PauseArmorBuffSteps);
        }

        [Test]
        public void ShieldAllies_ExpiresWhenNextPauseFires_EvenWithNoCommandsThere()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.ShieldAllies),
                TestBoards.FrontLineAnchor(4),
                "player_shield");
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(5), "player_rifle");
            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(4), "enemy_rifle_1");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(5), "enemy_rifle_2");

            var run = TickCombatRun.Start(player, enemy, seed: 13, authority: 2);

            var first = run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.ShieldAllies,
                    SourcePieceId = "player_shield"
                }
            });

            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "shield_allies" && e.TargetId == "player_rifle"),
                "ShieldAllies must have granted armor at the opening pause");
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status,
                "matched rifle lines should fire the mid-fight pause");

            var buffed = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_rifle");
            Assert.AreEqual(0, buffed.PauseArmorBuffSteps,
                "pause-granted armor expires the moment the next pause boundary fires — before any commands");

            run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(0, buffed.PauseArmorBuffSteps,
                "an uncommanded pause must not resurrect or retain pause-granted armor");
        }

        [Test]
        public void ShieldAllies_OnFightStartArmor_RevertsToBaselineAtNextPause()
        {
            var run = StartMatchedFightWithArmoredBuddy(authority: 2, buddyShieldSource: true);
            var buddy = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_buddy");
            Assert.AreEqual(1, buddy.ArmorBuffSteps, "medic aura should grant fight-start armor");

            var first = run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.ShieldAllies,
                    SourcePieceId = "player_shield"
                }
            });

            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "shield_allies" && e.TargetId == "player_buddy"),
                "ShieldAllies must have granted armor to the adjacent buddy");
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status,
                "matched rifle lines should fire the mid-fight pause");
            Assert.AreEqual(1, buddy.ArmorBuffSteps, "fight-start baseline is untouched by the pause boundary");
            Assert.AreEqual(0, buddy.PauseArmorBuffSteps, "pause-granted armor is gone at the next pause");
            Assert.AreEqual(1, buddy.TotalArmorSteps,
                "after the next pause the unit is back to exactly its fight-start baseline, not zero");
        }

        [Test]
        public void ShieldAllies_ReplayViaFastForward_ReproducesIdenticalLog()
        {
            var commands = new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.ShieldAllies,
                    SourcePieceId = "player_shield"
                }
            };

            var live = StartMatchedFightWithArmoredBuddy(authority: 2, buddyShieldSource: true);
            var liveResult = live.Continue(commands);
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, liveResult.Status);
            live.Continue(System.Array.Empty<PhaseCommand>());

            var restored = StartMatchedFightWithArmoredBuddy(authority: 2, buddyShieldSource: true);
            restored.FastForwardFromSave(checkpointsFired: 1, savedAwaitingCommand: true, submittedCommands: commands);
            restored.Continue(System.Array.Empty<PhaseCommand>());

            CollectionAssert.AreEqual(
                live.Log.Events.Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}").ToList(),
                restored.Log.Events.Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}").ToList(),
                "armor lifetime (fight-start + pause-granted) must reproduce identically through save-resume replay");
        }

        /// <summary>Matched rifle lines (fires the 60% mid pause) plus an immobile 3hp
        /// conscript parked deep in the enemy support zone (the deepest zone that accepts
        /// Units) — away from the front through segment 0, a deterministic mortar victim
        /// at the mid pause (MortarShot costs 3 there).</summary>
        private static TickCombatRun StartMatchedFightWithRearConscript(int authority)
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot),
                TestBoards.FrontLineAnchor(3),
                "player_grenadier");
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "player_rifle_2");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "enemy_rifle_1");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "enemy_rifle_2");
            Assert.IsTrue(
                enemy.TryPlace(
                    TestPieces.With(TestPieces.WeakConscript(), movementSpeed: 0),
                    TestBoards.SupportLineAnchor(1, 0),
                    "enemy_weak").Success,
                "rear conscript must be placed (support zone is the deepest zone that accepts Units)");

            return TickCombatRun.Start(player, enemy, seed: 42, authority: authority);
        }

        /// <summary>Matched rifle lines (fires the 60% mid pause) plus a medic-with-armor-aura
        /// next to an infantry buddy in the player support zone, so "player_buddy" starts the
        /// fight with 1 step of fight-start armor. Optional adjacent ShieldAllies source lets
        /// pause-granted armor stack on top of that baseline.</summary>
        private static TickCombatRun StartMatchedFightWithArmoredBuddy(
            int authority,
            bool buddyShieldSource = false)
        {
            var medic = TestPieces.With(
                TestPieces.CreateUnit("medic"),
                abilities: new[]
                {
                    new PieceAbilityDefinition
                    {
                        Id = "medic_adjacent_infantry_armor_plus_one",
                        Trigger = PieceAbilityTrigger.AdjacentAura,
                        NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                        Stat = SynergyStat.ArmorType,
                        ModType = SynergyModType.Flat,
                        Magnitude = 1
                    }
                });
            var buddy = TestPieces.CreateUnit("infantry_buddy", primary: GameTagIds.Infantry);

            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot),
                TestBoards.FrontLineAnchor(3),
                "player_grenadier");
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "player_rifle_2");
            Assert.IsTrue(
                player.TryPlace(medic, TestBoards.SupportLineAnchor(1, 0), "player_medic").Success,
                "medic must be placed in the support zone (column 3 is rear, which rejects Units)");
            Assert.IsTrue(
                player.TryPlace(buddy, TestBoards.SupportLineAnchor(2, 0), "player_buddy").Success,
                "buddy must be placed adjacent to the medic");
            if (buddyShieldSource)
            {
                Assert.IsTrue(
                    player.TryPlace(
                        TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.ShieldAllies),
                        TestBoards.SupportLineAnchor(3, 0),
                        "player_shield").Success,
                    "shield source must be placed adjacent to the buddy");
            }

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "enemy_rifle_1");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "enemy_rifle_2");

            return TickCombatRun.Start(player, enemy, seed: 42, authority: authority);
        }
    }
}
