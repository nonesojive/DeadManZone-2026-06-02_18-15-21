using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Meta;
using DeadManZone.Core.Run;

namespace DeadManZone.Game
{
    public sealed partial class RunOrchestrator
    {
        public RunMetaTracker MetaTracker { get; } = new();

        private void ResetMetaForNewRun()
        {
            MetaTracker.ResetRun();
        }

        private void RecordSalvageMeta(int supplies) => MetaTracker.RecordSalvage(supplies);

        private void RecordCriticalMassIfTriggered()
        {
            var board = GetPlayerBoard();
            if (CriticalMassEngine.Evaluate(board).HasAnyActiveRule)
                MetaTracker.RecordCriticalMassTrigger();
        }

        private void ProcessFightEndMeta(bool playerWon, bool hqDamaged)
        {
            if (hqDamaged)
                MetaTracker.RecordHqDamaged();

            MetaTracker.RecordFightCompleted();

            foreach (var id in MetaTracker.EvaluateFightEndAchievements(playerWon))
                GrantAchievement(id);

            if (State.EmergencyDraftUsed)
                GrantAchievement(AchievementIds.EmergencyDraftUsed);
        }

        private void ProcessRunEndMeta(bool victory)
        {
            int score = ComputeRunScore(victory);
            foreach (var id in MetaTracker.EvaluateRunEndAchievements(victory, State.FactionId, State.Morale))
                GrantAchievement(id);

            if (victory)
            {
                MetaProgressionService.RecordVictory(State.FactionId, score, State.FightIndex);
                SteamIntegration.SubmitLeaderboardScore("gauntlet_score", score);
            }
        }

        private int ComputeRunScore(bool victory)
        {
            if (!victory || State == null)
                return 0;

            return State.Supplies + State.Morale + State.FightIndex * 100;
        }

        private static void GrantAchievement(string achievementId)
        {
            if (!MetaProgressionService.TryUnlockAchievement(achievementId))
                return;

            var def = default(AchievementDefinition);
            if (AchievementCatalog.TryFind(achievementId, out def))
                SteamIntegration.UnlockAchievement(def.SteamApiName);
        }
    }
}
