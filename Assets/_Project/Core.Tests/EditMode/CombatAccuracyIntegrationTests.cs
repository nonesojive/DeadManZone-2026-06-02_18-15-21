using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatAccuracyIntegrationTests
    {
        [Test]
        public void TickCombatRun_LogsGrazeOrMiss_WithLongRangeFixture()
        {
            bool found = false;
            for (int seed = 1; seed <= 300 && !found; seed++)
            {
                var player = new BoardState(TestBoards.Layout);
                var attacker = TestPieces.With(
                    TestPieces.RifleSquad(),
                    attackRange: AttackRangeTier.Long,
                    attackSpeed: AttackSpeedTier.Fast,
                    attackType: AttackType.Shredding);
                player.TryPlace(attacker, TestBoards.FrontLineAnchor(5), instanceId: "player_attacker");

                var enemy = TestBoards.WeakEnemyOnly();
                var run = TickCombatRun.Start(player, enemy, seed);
                var result = run.Continue(System.Array.Empty<PhaseCommand>());
                while (result.Status == CombatAdvanceStatus.AwaitingCommand && !run.IsFightOver)
                    result = run.Continue(System.Array.Empty<PhaseCommand>());

                foreach (var combatEvent in run.Log.Events)
                {
                    if (combatEvent.ActionType == "graze" || combatEvent.ActionType == "miss")
                    {
                        found = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(found, "Expected graze or miss within first 300 seeds for long-range shredder fixture.");
        }
    }
}
