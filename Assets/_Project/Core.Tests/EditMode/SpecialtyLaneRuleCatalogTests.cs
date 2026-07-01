using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class SpecialtyLaneRuleCatalogTests
    {
        private static BoardState EmptyBoard() => new BoardState(TestBoards.Layout);

        private static BoardState WithInfantry(int count)
        {
            var board = EmptyBoard();
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(board.TryPlace(
                    TestPieces.RifleSquad(),
                    new GridCoord(7, i),
                    $"rifle_{i}").Success);
            }

            return board;
        }

        [Test]
        public void Resolve_EmptyBoard_PrefersAssaultAndTank()
        {
            var context = SpecialtyLaneRuleCatalog.Resolve(EmptyBoard(), TestContentRegistry.Instance);

            CollectionAssert.AreEquivalent(
                new[] { GameTagIds.Assault, GameTagIds.Tank },
                context.PreferredCombatRoles);
            Assert.IsFalse(context.PreferBuildings);
            Assert.IsFalse(context.PreferVehicles);
            Assert.IsFalse(context.IsWildcard);
        }

        [Test]
        public void Resolve_TwoInfantryOnly_PrefersSupportAndSpotter()
        {
            var board = WithInfantry(2);

            var context = SpecialtyLaneRuleCatalog.Resolve(board, TestContentRegistry.Instance);

            CollectionAssert.AreEquivalent(
                new[] { GameTagIds.Support },
                context.PreferredCombatRoles);
            CollectionAssert.AreEquivalent(
                new[] { GameTagIds.Spotter },
                context.PreferredSynergyTags);
            Assert.IsFalse(context.IsWildcard);
        }

        [Test]
        public void Resolve_TwoInfantryAndArtillery_PrefersBuildings()
        {
            var board = WithInfantry(2);
            Assert.IsTrue(board.TryPlace(
                TestPieces.CreateUnit("field_gun", primary: GameTagIds.Structure, combatRole: GameTagIds.Artillery),
                TestBoards.SupportLineAnchor(0),
                "field_gun").Success);

            var context = SpecialtyLaneRuleCatalog.Resolve(board, TestContentRegistry.Instance);

            Assert.IsTrue(context.PreferBuildings);
            Assert.IsFalse(context.IsWildcard);
        }

        [Test]
        public void Resolve_FullComposition_IsWildcard()
        {
            var combat = WithInfantry(2);
            Assert.IsTrue(combat.TryPlace(
                TestPieces.CreateUnit("field_gun", primary: GameTagIds.Structure, combatRole: GameTagIds.Artillery),
                TestBoards.SupportLineAnchor(0),
                "field_gun").Success);
            Assert.IsTrue(combat.TryPlace(
                TestPieces.CreateUnit("transport", primary: GameTagIds.Vehicle, combatRole: GameTagIds.Tank),
                TestBoards.FrontLineAnchor(4),
                "transport").Success);

            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            Assert.IsTrue(hq.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0), "bunker_1").Success);
            Assert.IsTrue(hq.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 2), "depot_1").Success);
            var board = new BuildBoardSet { Combat = combat, Hq = hq }.ToAggregateBoard();

            var context = SpecialtyLaneRuleCatalog.Resolve(board, TestContentRegistry.Instance);

            Assert.IsTrue(context.IsWildcard);
        }

        [Test]
        public void MatchesPreferences_AssaultRole_MatchesContext()
        {
            var piece = TestPieces.CreateUnit("striker", combatRole: GameTagIds.Assault);
            var context = SpecialtyLaneRuleCatalog.Resolve(EmptyBoard(), TestContentRegistry.Instance);

            Assert.IsTrue(SpecialtyLaneRuleCatalog.MatchesPreferences(piece, context));
        }

        [Test]
        public void FilterPool_EmptyBoard_KeepsOnlyAssaultOrTankPieces()
        {
            var pool = new[]
            {
                TestPieces.CreateUnit("assault_piece", combatRole: GameTagIds.Assault),
                TestPieces.CreateUnit("tank_piece", combatRole: GameTagIds.Tank),
                TestPieces.CreateUnit("support_piece", combatRole: GameTagIds.Support, synergyTags: new[] { GameTagIds.Spotter })
            };

            var context = SpecialtyLaneRuleCatalog.Resolve(EmptyBoard(), TestContentRegistry.Instance);
            var filtered = SpecialtyLaneRuleCatalog.FilterPool(pool, context).ToList();

            Assert.AreEqual(2, filtered.Count);
            Assert.IsTrue(filtered.Any(p => p.Id == "assault_piece"));
            Assert.IsTrue(filtered.Any(p => p.Id == "tank_piece"));
        }
    }
}
