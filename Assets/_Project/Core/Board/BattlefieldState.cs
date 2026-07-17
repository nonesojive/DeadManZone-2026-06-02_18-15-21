using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class BattlefieldCell
    {
        public string InstanceId { get; init; }
        public PieceDefinition Definition { get; init; }
        public CombatSide Side { get; init; }
        public GridCoord Position { get; set; }

        /// <summary>2026-07-17 Oathborn transport tentpole (§2.5): pass-through of
        /// PlacedPiece.CarrierInstanceId — null = not cargo. Pure data plumbing (no rule here;
        /// TickCombatRun/TransportRules already own the actual embark/disembark behavior) so
        /// presentation can tell "starts embarked" apart from "starts on the field" without
        /// re-deriving it from the raw BoardState.</summary>
        public string CarrierInstanceId { get; init; }
    }

    public sealed class BattlefieldState
    {
        public BattlefieldLayout Layout { get; }
        public IReadOnlyList<BattlefieldCell> Cells => _cells;

        private readonly List<BattlefieldCell> _cells = new();

        public BattlefieldState(BattlefieldLayout layout) => Layout = layout;

        public static BattlefieldState FromBoards(BoardState playerBoard, BoardState enemyBoard)
        {
            var layout = BattlefieldLayout.FromPlayerBoard(playerBoard.Layout);
            var state = new BattlefieldState(layout);

            foreach (var piece in playerBoard.Pieces)
            {
                state._cells.Add(new BattlefieldCell
                {
                    InstanceId = piece.InstanceId,
                    Definition = piece.Definition,
                    Side = CombatSide.Player,
                    Position = piece.Anchor,
                    CarrierInstanceId = piece.CarrierInstanceId
                });
            }

            foreach (var piece in enemyBoard.Pieces)
            {
                state._cells.Add(new BattlefieldCell
                {
                    InstanceId = piece.InstanceId,
                    Definition = piece.Definition,
                    Side = CombatSide.Enemy,
                    Position = new GridCoord(
                        layout.EnemyOriginX + layout.MirrorEnemyAnchorX(
                            piece.Anchor.X,
                            piece.Definition.Shape,
                            piece.Rotation),
                        piece.Anchor.Y),
                    CarrierInstanceId = piece.CarrierInstanceId
                });
            }

            return state;
        }

        public BattlefieldCell FindCell(string instanceId) =>
            _cells.Find(c => c.InstanceId == instanceId);
    }
}
