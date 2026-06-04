namespace DeadManZone.Game
{
    public readonly struct FightReward
    {
        public int Supplies { get; }
        public int BonusAuthority { get; }
        public int BonusManpower { get; }

        public FightReward(int supplies, int bonusAuthority, int bonusManpower)
        {
            Supplies = supplies;
            BonusAuthority = bonusAuthority;
            BonusManpower = bonusManpower;
        }
    }

    public static class FightRewardTable
    {
        private static readonly FightReward[] Rewards =
        {
            new FightReward(15, 1, 2),
            new FightReward(18, 1, 2),
            new FightReward(20, 1, 2),
            new FightReward(22, 2, 2),
            new FightReward(25, 2, 3),
            new FightReward(28, 2, 3),
            new FightReward(30, 2, 3),
            new FightReward(32, 3, 3),
            new FightReward(35, 3, 4),
            new FightReward(45, 4, 4)
        };

        public static FightReward GetReward(int fightIndex, bool isDraw = false)
        {
            int index = System.Math.Clamp(fightIndex - 1, 0, Rewards.Length - 1);
            var reward = Rewards[index];
            if (!isDraw)
                return reward;

            return new FightReward(
                System.Math.Max(1, reward.Supplies / 2),
                reward.BonusAuthority,
                reward.BonusManpower);
        }
    }
}
