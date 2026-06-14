using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class CombatReplayStateTests
    {
        [Test]
        public void ResetFromBattlefield_RegistersAllCellPositions()
        {
            var battlefield = BattlefieldState.FromBoards(TestBoards.StandardPlayer(), TestBoards.StandardEnemy());
            var state = new CombatReplayState();

            state.ResetFromBattlefield(battlefield);

            foreach (var cell in battlefield.Cells)
            {
                Assert.IsTrue(state.TryGetAnchor(cell.InstanceId, out var anchor));
                Assert.AreEqual(cell.Position, anchor);
            }
        }

        [Test]
        public void ApplyEvent_Move_UpdatesAnchor()
        {
            var battlefield = BattlefieldState.FromBoards(TestBoards.StandardPlayer(), TestBoards.StandardEnemy());
            var state = new CombatReplayState();
            state.ResetFromBattlefield(battlefield);

            var destination = new GridCoord(6, 5);
            var moved = state.ApplyEvent(new CombatEvent
            {
                ActionType = "move",
                ActorId = "player_rifle_1",
                TargetId = $"{destination.X},{destination.Y}"
            });

            Assert.IsTrue(moved);
            Assert.IsTrue(state.TryGetAnchor("player_rifle_1", out var anchor));
            Assert.AreEqual(destination, anchor);
        }

        [Test]
        public void ApplyEvent_Destroyed_RemovesAnchor()
        {
            var battlefield = BattlefieldState.FromBoards(TestBoards.StandardPlayer(), TestBoards.StandardEnemy());
            var state = new CombatReplayState();
            state.ResetFromBattlefield(battlefield);

            Assert.IsTrue(state.ApplyEvent(new CombatEvent
            {
                ActionType = "destroyed",
                ActorId = "player_rifle_1"
            }));

            Assert.IsFalse(state.TryGetAnchor("player_rifle_1", out _));
        }

        [Test]
        public void RestoreFromBattlefieldAndEvents_SkipsExcludedSegment()
        {
            var battlefield = BattlefieldState.FromBoards(TestBoards.StandardPlayer(), TestBoards.StandardEnemy());
            var state = new CombatReplayState();
            var events = new List<CombatEvent>
            {
                new()
                {
                    Segment = 0,
                    Tick = 1,
                    ActionType = "move",
                    ActorId = "player_rifle_1",
                    TargetId = "6,5"
                },
                new()
                {
                    Segment = 1,
                    Tick = 1,
                    ActionType = "move",
                    ActorId = "player_rifle_1",
                    TargetId = "5,5"
                }
            };

            state.RestoreFromBattlefieldAndEvents(battlefield, events, excludeSegment: 1);

            Assert.IsTrue(state.TryGetAnchor("player_rifle_1", out var anchor));
            Assert.AreEqual(new GridCoord(6, 5), anchor);
        }
    }
}
