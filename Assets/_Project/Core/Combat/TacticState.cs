namespace DeadManZone.Core.Combat
{
    public sealed class TacticState
    {
        public TacticType PlayerTactic { get; set; } = TacticType.DisciplinedFire;
        public TacticType EnemyTactic { get; set; } = TacticType.DisciplinedFire;
        public int PlayerDamageBuff { get; set; }
        public int EnemyDamageBuff { get; set; }
    }
}
