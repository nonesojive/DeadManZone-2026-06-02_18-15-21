namespace DeadManZone.Core.Combat
{
    public sealed class StanceState
    {
        public StanceType PlayerStance { get; set; } = StanceType.FocusWeakest;
        public StanceType EnemyStance { get; set; } = StanceType.FocusWeakest;
        public int PlayerDamageBuff { get; set; }
        public int EnemyDamageBuff { get; set; }
    }
}
