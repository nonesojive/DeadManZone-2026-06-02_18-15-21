using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class BoardStateTests
    {
        private static BoardLayout DefaultLayout() =>
            BoardLayout.CreateHorizontalZones(
                width: TestBoards.DefaultWidth,
                height: TestBoards.DefaultHeight,
                rearCols: TestBoards.DefaultRearCols,
                supportCols: TestBoards.DefaultSupportCols,
                specialTiles: new[]
                {
                    new GridCoord(1, 4),
                    new GridCoord(4, 4)
                });

        [Test]
        public void CreateHorizontalZones_RearIsLeftmostColumn()
        {
            var layout = BoardLayout.CreateHorizontalZones(
                9,
                10,
                rearCols: 4,
                supportCols: 3,
                new[] { new GridCoord(1, 4) });
            Assert.AreEqual(ZoneType.Rear, layout.GetZone(new GridCoord(0, 5)));
            Assert.AreEqual(ZoneType.Front, layout.GetZone(new GridCoord(8, 5)));
        }

        [Test]
        public void CanPlace_ReturnsFalseForInvalidZone()
        {
            var board = new BoardState(DefaultLayout());
            Assert.IsFalse(board.CanPlace(TestPieces.RifleSquad(), new GridCoord(0, 0)));
        }

        [Test]
        public void CannotPlaceUnitInRearZone()
        {
            var board = new BoardState(DefaultLayout());
            var result = board.TryPlace(TestPieces.RifleSquad(), new GridCoord(0, 0));
            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("zone").IgnoreCase);
        }

        [Test]
        public void BuildingOnSpecialTile_FlagsSpecialBonus()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new GridCoord(1, 4)).Success);
            Assert.IsTrue(board.IsOnSpecialTile(board.Pieces.First().InstanceId));
        }

        [Test]
        public void RemovePiece_FreesCellsForPlacement()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            var anchor = new GridCoord(1, 4);
            Assert.IsTrue(board.TryPlace(bunker, anchor, "bunker_1").Success);

            Assert.IsTrue(board.TryRemove("bunker_1", out var removed));
            Assert.AreEqual("bunker_1", removed.InstanceId);
            Assert.IsEmpty(board.Pieces);

            var retry = board.TryPlace(bunker, anchor, "bunker_2");
            Assert.IsTrue(retry.Success, retry.Reason);
        }

        [Test]
        public void TryRelocate_MovesPieceToValidAnchor()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new GridCoord(1, 4), "bunker_1").Success);

            var result = board.TryRelocate("bunker_1", new GridCoord(0, 4), PieceRotation.R0);

            Assert.IsTrue(result.Success, result.Reason);
            Assert.AreEqual(1, board.Pieces.Count);
            Assert.AreEqual(new GridCoord(0, 4), board.Pieces.First().Anchor);
        }

        [Test]
        public void TryRelocate_RejectsInvalidZoneAndRestoresPosition()
        {
            var board = new BoardState(DefaultLayout());
            var bunker = TestPieces.CommandBunker();
            var anchor = new GridCoord(1, 4);
            Assert.IsTrue(board.TryPlace(bunker, anchor, "bunker_1").Success);

            var result = board.TryRelocate("bunker_1", new GridCoord(7, 4), PieceRotation.R0);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(anchor, board.Pieces.First().Anchor);
        }

        [Test]
        public void CanPlace_R90UnitOccupiesRotatedCells()
        {
            var board = new BoardState(DefaultLayout());
            var unit = new PieceDefinition
            {
                Id = "wide_unit",
                DisplayName = "Wide Unit",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
                MaxHp = 10
            };
            var anchor = TestBoards.FrontLineAnchor();

            Assert.IsTrue(board.TryPlace(unit, anchor, "unit_1", PieceRotation.R90).Success);
            Assert.AreEqual(PieceRotation.R90, board.Pieces.First().Rotation);

            Assert.IsFalse(board.CanPlace(unit, anchor));
            Assert.IsFalse(board.CanPlace(unit, new GridCoord(anchor.X, anchor.Y + 1)));
            Assert.IsTrue(board.CanPlace(unit, new GridCoord(anchor.X, anchor.Y - 1)));
        }

        [Test]
        public void CombatBoard_6x6_HasNoZoneRestrictionsForUnits()
        {
            var board = new BoardState(TestBoards.CombatLayout);
            Assert.AreEqual(BoardKind.Combat, board.Layout.Kind);
            Assert.IsFalse(board.Layout.UsesZones);

            Assert.IsTrue(board.TryPlace(TestPieces.RifleSquad(), new GridCoord(0, 0)).Success);
            Assert.IsTrue(board.TryPlace(TestPieces.RifleSquad(), new GridCoord(5, 5), instanceId: "rifle_2").Success);
        }

        [Test]
        public void HqBoard_RejectsPlacementOnBlockedCells()
        {
            var board = new BoardState(TestBoards.HqLayoutWithBlockedCorner());
            var depot = TestPieces.SupplyDepot();

            var blocked = board.TryPlace(depot, new GridCoord(0, 0));
            Assert.IsFalse(blocked.Success);
            Assert.That(blocked.Reason, Does.Contain("blocked").IgnoreCase);

            Assert.IsTrue(board.TryPlace(depot, new GridCoord(2, 0)).Success);
        }

        [Test]
        public void Buildings_OnlyAllowedOnHqBoard()
        {
            var combat = new BoardState(TestBoards.CombatLayout);
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var bunker = TestPieces.CommandBunker();

            var onCombat = combat.TryPlace(bunker, new GridCoord(0, 0));
            Assert.IsFalse(onCombat.Success);
            Assert.That(onCombat.Reason, Does.Contain("HQ board").IgnoreCase);

            Assert.IsTrue(hq.TryPlace(bunker, new GridCoord(0, 0)).Success);
        }

        [Test]
        public void Units_OnlyAllowedOnCombatBoard()
        {
            var combat = new BoardState(TestBoards.CombatLayout);
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var rifle = TestPieces.RifleSquad();

            var onHq = hq.TryPlace(rifle, new GridCoord(0, 0));
            Assert.IsFalse(onHq.Success);
            Assert.That(onHq.Reason, Does.Contain("combat board").IgnoreCase);

            Assert.IsTrue(combat.TryPlace(rifle, new GridCoord(0, 0)).Success);
        }
    }
}
