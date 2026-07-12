using DeadManZone.Core.Board;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;

namespace DeadManZone.Game
{
    public sealed partial class RunOrchestrator
    {
        public RoundIncomePreview GetNextCombatIncomePreview()
        {
            var boards = GetBuildBoards();
            return RoundIncomeCalculator.ComputePreview(
                Faction.baseSuppliesPerRound,
                Faction.baseMusterPerShop,
                Faction.baseSalvageChancePercent,
                boards,
                GetCombatBoard());
        }

        public void SyncSalvageChancePercent()
        {
            if (Faction == null || State == null)
                return;

            // Kill-share scaling (ADR-0005): routed enemies escaped with their gear, so
            // the build round after an all-rout fight salvages nothing. The percent is
            // state (stamped by CompleteCombat), so the per-round re-syncs from
            // RefreshShop and the HUD can't erase it.
            State.SalvageChancePercent = SalvageChanceCalculator.ApplyKillShare(
                SalvageChanceCalculator.Compute(
                    Faction.baseSalvageChancePercent,
                    SalvageBoardBoostAggregator.SumBoardBoost(GetCombatBoard())),
                State.LastFightSalvageKillPercent);
        }

        private int ApplyPostCombatIncome()
        {
            int suppliesIncome = RoundIncomeCalculator.ComputeSuppliesIncome(
                Faction.baseSuppliesPerRound,
                GetBuildBoards());
            State.Supplies += suppliesIncome;
            ApplyMuster();
            return suppliesIncome;
        }
    }
}
