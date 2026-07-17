using System.Collections.Generic;
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

            if (State.CombatBoard == null)
                return new BoardState(Faction.CreateCombatBoardLayout());

            // 2026-07-17 round-2 playtest fix: a save made before the cargo-hold-fit rule
            // existed can list more cargo than now fits its transport's 2x2 hold. Never throw
            // on load — evict the excess to reserves (or leave it on the board) and log it.
            var warnings = new List<string>();
            var board = BoardSnapshotMapper.ToBoard(State.CombatBoard, _registry, GetReserves(), warnings);
            LogCargoEvictionWarnings(warnings);
            return board;
        }

        public BoardState GetHqBoard()
        {
            if (Faction == null)
                return new BoardState(BoardLayout.CreateHqBoard(3, 6));

            if (State.HqBoard == null)
                return new BoardState(Faction.CreateHqBoardLayout());

            var warnings = new List<string>();
            var board = BoardSnapshotMapper.ToBoard(State.HqBoard, _registry, GetReserves(), warnings);
            LogCargoEvictionWarnings(warnings);
            return board;
        }

        private static void LogCargoEvictionWarnings(List<string> warnings)
        {
            if (warnings == null)
                return;

            foreach (var warning in warnings)
                UnityEngine.Debug.LogWarning($"[DeadManZone] {warning}");
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
