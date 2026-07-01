using DeadManZone.Core.Board;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;

namespace DeadManZone.Game
{
    public sealed partial class RunOrchestrator
    {
        public RoundIncomePreview GetNextCombatIncomePreview()
        {
            int baseSupplies = FightRewardTable.GetReward(State.FightIndex).Supplies;
            var boards = GetBuildBoards();
            return RoundIncomeCalculator.ComputePreview(
                baseSupplies,
                Faction.baseMusterPerShop,
                Faction.baseSalvageChancePercent,
                boards,
                GetCombatBoard());
        }

        public void SyncSalvageChancePercent()
        {
            if (Faction == null || State == null)
                return;

            State.SalvageChancePercent = SalvageChanceCalculator.Compute(
                Faction.baseSalvageChancePercent,
                SalvageBoardBoostAggregator.SumBoardBoost(GetCombatBoard()));
        }

        private int ApplyPostCombatIncome()
        {
            int baseSupplies = FightRewardTable.GetReward(State.FightIndex).Supplies;
            int suppliesIncome = RoundIncomeCalculator.ComputeSuppliesIncome(baseSupplies, GetBuildBoards());
            State.Supplies += suppliesIncome;
            ApplyMuster();
            return suppliesIncome;
        }
    }
}
