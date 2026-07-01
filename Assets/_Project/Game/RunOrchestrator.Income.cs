using DeadManZone.Core.Board;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;

namespace DeadManZone.Game
{
    public sealed partial class RunOrchestrator
    {
        public RoundIncomePreview GetNextCombatIncomePreview()
        {
            int fightRewardSupplies = FightRewardTable.GetReward(State.FightIndex).Supplies;
            var boards = GetBuildBoards();
            return RoundIncomeCalculator.ComputePreview(
                fightRewardSupplies,
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

            State.SalvageChancePercent = SalvageChanceCalculator.Compute(
                Faction.baseSalvageChancePercent,
                SalvageBoardBoostAggregator.SumBoardBoost(GetCombatBoard()));
        }

        private int ApplyPostCombatIncome()
        {
            int fightRewardSupplies = FightRewardTable.GetReward(State.FightIndex).Supplies;
            int suppliesIncome = RoundIncomeCalculator.ComputeSuppliesIncome(
                fightRewardSupplies,
                Faction.baseSuppliesPerRound,
                GetBuildBoards());
            State.Supplies += suppliesIncome;
            ApplyMuster();
            return suppliesIncome;
        }
    }
}
