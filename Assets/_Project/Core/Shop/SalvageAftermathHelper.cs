using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;

namespace DeadManZone.Core.Shop
{
    public static class SalvageAftermathHelper
    {
        public static int CountDestroyedEnemyTypes(CombatEventLog eventLog, BoardSnapshot enemyBoard)
        {
            if (eventLog?.Events == null || enemyBoard?.Pieces == null || enemyBoard.Pieces.Count == 0)
                return 0;

            var enemyInstanceToPiece = enemyBoard.Pieces
                .Where(p => !string.IsNullOrEmpty(p.InstanceId))
                .ToDictionary(p => p.InstanceId, p => p.PieceId, StringComparer.Ordinal);

            var destroyedTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var combatEvent in eventLog.Events)
            {
                if (combatEvent.ActionType != "destroyed")
                    continue;

                if (enemyInstanceToPiece.TryGetValue(combatEvent.ActorId, out string pieceId))
                    destroyedTypes.Add(pieceId);
            }

            return destroyedTypes.Count;
        }

        public static FightOutcome ResolveOutcome(bool playerWon, bool isDraw)
        {
            if (isDraw)
                return FightOutcome.Draw;

            return playerWon ? FightOutcome.Victory : FightOutcome.Defeat;
        }
    }

}
