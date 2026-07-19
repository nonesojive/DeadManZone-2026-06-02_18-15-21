using System;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using UnityEngine;

namespace DeadManZone.Data
{
    [Serializable]
    public struct EnemyPiecePlacement
    {
        public PieceDefinitionSO piece;
        public Vector2Int anchor;
        public string instanceId;
    }

    [CreateAssetMenu(menuName = "DeadManZone/Enemy Template")]
    public class EnemyTemplateSO : ScriptableObject
    {
        public int fightNumber = 1;
        public string displayName;
        public string previewTag;
        public string enemyFactionId = "crimson_assembly";
        public EnemyPiecePlacement[] placements;

        public BoardSnapshot ToBoardSnapshot(int boardSize = 6)
        {
            if (placements == null || placements.Length == 0)
                throw new InvalidOperationException($"Enemy template '{name}' has no placements.");

            var snapshot = new BoardSnapshot
            {
                BoardKind = BoardKind.Combat.ToString(),
                Width = boardSize,
                Height = boardSize,
                SpecialTiles = new System.Collections.Generic.List<GridCoordRecord>()
            };

            foreach (var placement in placements)
            {
                if (placement.piece == null)
                    continue;

                var anchor = EnemyPlacementLayout.RemapLegacyAnchor(
                    placement.anchor.x,
                    placement.anchor.y,
                    boardSize);

                snapshot.Pieces.Add(new PlacedPieceRecord
                {
                    InstanceId = string.IsNullOrEmpty(placement.instanceId)
                        ? $"enemy_{placement.piece.id}_{anchor.X}_{anchor.Y}"
                        : placement.instanceId,
                    PieceId = placement.piece.id,
                    AnchorX = anchor.X,
                    AnchorY = anchor.Y
                });
            }

            return snapshot;
        }

        public BoardState BuildBoard(FactionSO faction, ContentRegistry registry)
        {
            int boardSize = faction != null ? faction.combatBoardSize : 6;
            var board = BoardSnapshotMapper.ToBoard(ToBoardSnapshot(boardSize), registry);

            // PROVISIONAL 2026-07-19 owner spec (deep balance pass): normalize every template
            // board onto the canonical EnemyLadder curve at the single build choke point, so
            // EVERY consumer (FightOptionGenerator draws, RunOrchestrator fight-time rebuilds,
            // benchmarks, tests) sees the deliberate ladder instead of the accidental authored
            // per-faction curves. SolveScale leaves the board at the solved uniform per-piece
            // StatScale, on the default engines-on Evaluate basis. Composition (which pieces,
            // where) stays authored; only the strength is pinned.
            BoardStrengthScaler.SolveScale(board, EnemyLadder.TargetStrength(fightNumber));
            return board;
        }
    }
}
