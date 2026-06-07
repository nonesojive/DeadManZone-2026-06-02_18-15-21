using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SynergyEngineTests
    {
        [Test]
        public void Supply_OutboundAura_BuffsAdjacentNeighbor()
        {
            var supply = TestPieces.CreateUnit(
                "supply",
                synergyTags: new[] { GameTagIds.Supply },
                tags: new[] { GameKeywords.Supply });
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant,
                tags: new[] { GameKeywords.Infantry, GameTags.Combatant });
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(supply, new GridCoord(0, 0), "supply_1").Success);
            Assert.IsTrue(board.TryPlace(rifle, new GridCoord(1, 0), "rifle_1").Success);

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("rifle_1", out var rifleSynergy));
            Assert.AreEqual(1, rifleSynergy.DamageBonus);

            var combatant = new CombatantState
            {
                InstanceId = "rifle_1",
                Definition = rifle,
                CurrentHp = rifle.MaxHp
            };

            SynergyEngine.ApplyToCombatants(snapshot, new[] { combatant });
            Assert.AreEqual(1, combatant.DamageBonus);
        }

        [Test]
        public void FightStartSnapshot_DoesNotChangeAfterRelocate()
        {
            var supply = TestPieces.CreateUnit(
                "supply",
                synergyTags: new[] { GameTagIds.Supply },
                tags: new[] { GameKeywords.Supply });
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant,
                tags: new[] { GameKeywords.Infantry, GameTags.Combatant });
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(supply, new GridCoord(0, 0), "supply_1").Success);
            Assert.IsTrue(board.TryPlace(rifle, new GridCoord(1, 0), "rifle_1").Success);

            var initialSnapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(initialSnapshot.TryGet("rifle_1", out var initialRifleResult));
            Assert.AreEqual(1, initialRifleResult.DamageBonus);

            var moved = board.TryRelocate("rifle_1", new GridCoord(4, 0), PieceRotation.R0);
            Assert.IsTrue(moved.Success, moved.Reason);

            var movedSnapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(movedSnapshot.TryGet("rifle_1", out var movedRifleResult));
            Assert.AreEqual(0, movedRifleResult.DamageBonus);

            var combatantWithInitialSnapshot = new CombatantState
            {
                InstanceId = "rifle_1",
                Definition = rifle,
                CurrentHp = rifle.MaxHp
            };
            SynergyEngine.ApplyToCombatants(initialSnapshot, new[] { combatantWithInitialSnapshot });
            Assert.AreEqual(1, combatantWithInitialSnapshot.DamageBonus);

            var combatantWithMovedSnapshot = new CombatantState
            {
                InstanceId = "rifle_1",
                Definition = rifle,
                CurrentHp = rifle.MaxHp
            };
            SynergyEngine.ApplyToCombatants(movedSnapshot, new[] { combatantWithMovedSnapshot });
            Assert.AreEqual(0, combatantWithMovedSnapshot.DamageBonus);
        }
    }
}
