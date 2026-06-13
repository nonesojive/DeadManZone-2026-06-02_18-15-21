using System;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ArmyHealthBarBattlefieldTests
    {
        [Test]
        public void BattlefieldRegistration_ThenSimEvents_ReducesArmyFraction()
        {
            var player = TestBoards.StandardPlayer();
            var enemy = TestBoards.StandardEnemy();
            var battlefield = BattlefieldState.FromBoards(player, enemy);

            var tracker = new ArmyHealthReplayTracker();
            RegisterCombatantsFromBattlefield(tracker, battlefield);

            Assert.Greater(tracker.GetFraction(CombatSide.Player), 0.99f);
            Assert.Greater(tracker.GetFraction(CombatSide.Enemy), 0.99f);

            var run = TickCombatRun.Start(player, enemy, seed: 4242, authority: 100);
            var result = run.Continue(Array.Empty<PhaseCommand>());

            foreach (var combatEvent in result.EventLog.Events.Where(e => e.Segment == result.SegmentIndex))
                tracker.ApplyEvent(combatEvent);

            bool playerDropped = tracker.GetFraction(CombatSide.Player) < 0.99f;
            bool enemyDropped = tracker.GetFraction(CombatSide.Enemy) < 0.99f;
            Assert.IsTrue(playerDropped || enemyDropped,
                "Expected at least one army bar fraction to drop after segment replay events.");
        }

        private static void RegisterCombatantsFromBattlefield(
            ArmyHealthReplayTracker tracker,
            BattlefieldState battlefield)
        {
            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;

                if (!PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    continue;

                tracker.RegisterUnit(cell.InstanceId, cell.Side, cell.Definition.MaxHp);
            }
        }
    }
}
