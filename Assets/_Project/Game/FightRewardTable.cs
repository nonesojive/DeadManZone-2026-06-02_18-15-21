namespace DeadManZone.Game
{
    public readonly struct FightReward
    {
        public int Gold { get; }
        public int Requisition { get; }

        public FightReward(int gold, int requisition)
        {
            Gold = gold;
            Requisition = requisition;
        }
    }

    public static class FightRewardTable
    {
        private static readonly FightReward[] Rewards =
        {
            new FightReward(15, 1),
            new FightReward(20, 1),
            new FightReward(25, 2),
            new FightReward(30, 2),
            new FightReward(40, 3)
        };

        public static FightReward GetReward(int fightIndex)
        {
            int index = System.Math.Clamp(fightIndex - 1, 0, Rewards.Length - 1);
            return Rewards[index];
        }
    }
}
