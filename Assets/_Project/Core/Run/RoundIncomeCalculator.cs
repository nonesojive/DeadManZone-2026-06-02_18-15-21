using System;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Shop;

namespace DeadManZone.Core.Run
{
    public readonly struct RoundIncomePreview
    {
        public int SuppliesIncome { get; }
        public int ManpowerIncome { get; }
        public int AuthorityPool { get; }
        public int SalvageChancePercent { get; }

        public RoundIncomePreview(
            int suppliesIncome,
            int manpowerIncome,
            int authorityPool,
            int salvageChancePercent)
        {
            SuppliesIncome = suppliesIncome;
            ManpowerIncome = manpowerIncome;
            AuthorityPool = authorityPool;
            SalvageChancePercent = salvageChancePercent;
        }
    }

    /// <summary>Post-combat income from faction baseline plus board bonuses.</summary>
    public static class RoundIncomeCalculator
    {
        public static int ComputeSuppliesIncome(int baseSupplies, BuildBoardSet boards)
        {
            if (baseSupplies <= 0)
                return Math.Max(0, ComputeBoardSuppliesBonus(0, boards));

            int boardBonus = ComputeBoardSuppliesBonus(baseSupplies, boards);
            return baseSupplies + boardBonus;
        }

        public static int ComputeBoardSuppliesBonus(int baseSupplies, BuildBoardSet boards)
        {
            var snapshot = CriticalMassEngine.Evaluate(boards);
            int percentBonus = baseSupplies > 0
                ? (int)Math.Round(baseSupplies * (snapshot.SuppliesPercentBonus / 100f))
                : 0;
            return snapshot.SuppliesFlatBonus + percentBonus;
        }

        public static int ComputeManpowerIncome(int baseMusterPerShop, BuildBoardSet boards) =>
            MusterCalculator.Compute(boards?.ToAggregateBoard(), baseMusterPerShop);

        public static int ComputeAuthorityPool(BuildBoardSet boards) =>
            AuthorityCalculator.ComputeRoundPool(boards);

        public static int ComputeSalvageChancePreview(int baseSalvagePercent, BoardState combatBoard)
        {
            int boardBoost = SalvageBoardBoostAggregator.SumBoardBoost(combatBoard);
            return SalvageChanceCalculator.Compute(baseSalvagePercent, boardBoost);
        }

        public static RoundIncomePreview ComputePreview(
            int baseSupplies,
            int baseMusterPerShop,
            int baseSalvagePercent,
            BuildBoardSet boards,
            BoardState combatBoard) =>
            new(
                ComputeSuppliesIncome(baseSupplies, boards),
                ComputeManpowerIncome(baseMusterPerShop, boards),
                ComputeAuthorityPool(boards),
                ComputeSalvageChancePreview(baseSalvagePercent, combatBoard));

        public static string FormatIncomeLabel(int total) =>
            total >= 0 ? $"+{total}" : total.ToString();
    }
}
