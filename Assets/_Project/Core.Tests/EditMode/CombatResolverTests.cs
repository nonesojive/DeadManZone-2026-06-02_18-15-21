using System.Linq;
using DeadManZone.Core.Combat;
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
            var enemy = TestBoards.WeakEnemyOnly();

            var result = resolver.Resolve(player, enemy, seed: 99, commands: System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(result.PlayerWon);
            Assert.IsTrue(result.EventLog.Events.Any(e => e.ActionType == "damage"));
        }

        [Test]
        public void StanceCommand_IsLoggedBetweenPhases()
        {
            var resolver = new CombatResolver();
            var player = TestBoards.WithCommandBunker();
            var enemy = TestBoards.StandardEnemy();

            var commands = new[]
            {
                new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.ChangeStance,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = player.Pieces.First(p => p.Definition.Id == "command_bunker").InstanceId
                }
            };

            var result = resolver.Resolve(player, enemy, seed: 7, commands: commands);

            Assert.IsTrue(result.EventLog.Events.Any(e => e.ActionType == "stance_change"));
        }
    }
}
