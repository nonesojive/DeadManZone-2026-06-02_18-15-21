using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class CombatResolverTests
    {
        [Test]
        public void SameSeedAndBoards_IdenticalEventLog()
        {
            var resolver = new CombatResolver();
            var boardA = TestBoards.StandardPlayer();
            var boardB = TestBoards.StandardEnemy();

            var result1 = resolver.Resolve(boardA, boardB, seed: 42, commands: System.Array.Empty<PhaseCommand>());
            var result2 = resolver.Resolve(boardA, boardB, seed: 42, commands: System.Array.Empty<PhaseCommand>());

            Assert.AreEqual(result1.EventLog.Events.Count, result2.EventLog.Events.Count);
            for (int i = 0; i < result1.EventLog.Events.Count; i++)
            {
                Assert.AreEqual(result1.EventLog.Events[i].ActionType, result2.EventLog.Events[i].ActionType);
                Assert.AreEqual(result1.EventLog.Events[i].Value, result2.EventLog.Events[i].Value);
            }
        }

        [Test]
        public void StrongerArmy_WinsCombat()
        {
            var resolver = new CombatResolver();
            var player = TestBoards.StrongPlayerVsWeakEnemy();
            player.TryPlace(
                TestPieces.With(
                    TestPieces.RifleSquad(),
                    baseDamage: 100,
                    attackSpeed: AttackSpeedTier.Fast,
                    cooldownTicks: 1,
                    accuracyOverride: 100),
                new GridCoord(7, 3),
                instanceId: "player_rifle_3");
            var enemy = TestBoards.WeakEnemyOnly();

            var result = resolver.Resolve(player, enemy, seed: 99, commands: System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(result.PlayerWon, "Overwhelming force should still win despite accuracy variance.");
            Assert.IsTrue(
                result.EventLog.Events.Any(e => e.ActionType == "damage" || e.ActionType == "graze"),
                "Expected at least one damaging hit or graze in the log.");
        }

        [Test]
        public void StanceCommand_IsLoggedBetweenPhases()
        {
            var board = TestBoards.WithCommandBunker();
            var processor = new CommandProcessor();
            var log = new CombatEventLog();
            var tactics = new TacticState();
            int authority = 2;
            var commands = new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = "player_tactic"
                }
            };

            var result = processor.TryApplyBatch(
                commands,
                board,
                ref authority,
                tactics,
                playerCombatants: new List<CombatantState>(),
                enemyCombatants: new List<CombatantState>(),
                log,
                checkpointIndex: 0,
                globalTick: 0);

            Assert.IsTrue(result.Success, result.Reason);
            Assert.IsTrue(log.Events.Any(e => e.ActionType == "tactic_set"));
        }
    }
}
