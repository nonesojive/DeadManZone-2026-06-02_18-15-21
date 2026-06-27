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
        public string enemyFactionId = "crimson_legion";
        public EnemyPiecePlacement[] placements;

        public BoardSnapshot ToBoardSnapshot()
        {
            if (placements == null || placements.Length == 0)
                throw new InvalidOperationException($"Enemy template '{name}' has no placements.");

            var snapshot = new BoardSnapshot
            {
                BoardKind = BoardKind.Combat.ToString(),
                Width = 6,
                Height = 6,
                SpecialTiles = new System.Collections.Generic.List<GridCoordRecord>()
            };

            foreach (var placement in placements)
            {
                if (placement.piece == null)
                    continue;

                snapshot.Pieces.Add(new PlacedPieceRecord
                {
                    InstanceId = string.IsNullOrEmpty(placement.instanceId)
                        ? $"enemy_{placement.piece.id}_{placement.anchor.x}_{placement.anchor.y}"
                        : placement.instanceId,
                    PieceId = placement.piece.id,
                    AnchorX = placement.anchor.x,
                    AnchorY = placement.anchor.y
                });
            }

            return snapshot;
        }

        public BoardState BuildBoard(FactionSO faction, ContentRegistry registry)
        {
            var snapshot = ToBoardSnapshot();
            snapshot.Width = faction.combatBoardSize;
            snapshot.Height = faction.combatBoardSize;
            return BoardSnapshotMapper.ToBoard(snapshot, registry);
        }
    }
}
