using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;

namespace DeadManZone.Game
{
    public sealed partial class RunOrchestrator
    {
        public BuildBoardSet GetBuildBoards() =>
            new BuildBoardSet
            {
                Combat = GetCombatBoard(),
                Hq = GetHqBoard()
            };

        public BoardState GetCombatBoard()
        {
            if (Faction == null)
                return new BoardState(BoardLayout.CreateCombatBoard());

            return State.CombatBoard == null
                ? new BoardState(Faction.CreateCombatBoardLayout())
                : BoardSnapshotMapper.ToBoard(State.CombatBoard, _registry);
        }

        public BoardState GetHqBoard()
        {
            if (Faction == null)
                return new BoardState(BoardLayout.CreateHqBoard(6, 3));

            return State.HqBoard == null
                ? new BoardState(Faction.CreateHqBoardLayout())
                : BoardSnapshotMapper.ToBoard(State.HqBoard, _registry);
        }

        public void SaveCombatBoard(BoardState board)
        {
            State.CombatBoard = BoardSnapshotMapper.FromBoard(board);
            Persist();
        }

        public void SaveHqBoard(BoardState board)
        {
            State.HqBoard = BoardSnapshotMapper.FromBoard(board);
            Persist();
        }

        /// <summary>Combat board only — use for combat sim and arena binding.</summary>
        public BoardState GetPlayerBoard() => GetCombatBoard();

        public void SavePlayerBoard(BoardState board) => SaveCombatBoard(board);

        private BoardState GetShopBoard() => GetBuildBoards().ToAggregateBoard();

        private BoardState GetBoardForPiece(PieceDefinition piece) =>
            BoardPlacementRules.ResolveTargetBoard(piece) == BoardKind.Hq
                ? GetHqBoard()
                : GetCombatBoard();

        private void SaveBoardForPiece(PieceDefinition piece, BoardState board)
        {
            if (BoardPlacementRules.ResolveTargetBoard(piece) == BoardKind.Hq)
                SaveHqBoard(board);
            else
                SaveCombatBoard(board);
        }

        private bool TryFindPlacedPiece(string instanceId, out BoardState board, out PlacedPiece piece)
        {
            board = null;
            piece = null;

            var combat = GetCombatBoard();
            var match = combat.Pieces.FirstOrDefault(p => p.InstanceId == instanceId);
            if (match != null)
            {
                board = combat;
                piece = match;
                return true;
            }

            var hq = GetHqBoard();
            match = hq.Pieces.FirstOrDefault(p => p.InstanceId == instanceId);
            if (match != null)
            {
                board = hq;
                piece = match;
                return true;
            }

            return false;
        }
    }
}
