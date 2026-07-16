namespace DeadManZone.Core.Combat
{
    public sealed class TacticState
    {
        public TacticType PlayerTactic { get; set; } = TacticType.DisciplinedFire;
        public TacticType EnemyTactic { get; set; } = TacticType.DisciplinedFire;
        public int PlayerDamageBuff { get; set; }
        public int EnemyDamageBuff { get; set; }

        /// <summary>2026-07-15 faction-roster-v1 §2.6 Resonance Coil's Echo: the last
        /// successfully-executed non-Echo UseAbility command this fight, for Echo to replay for
        /// free. Null until the first ability fires. Scoped to abilities only this wave (not
        /// SetTactic — see CommandProcessor.TryApplyBatch).</summary>
        public PhaseCommand LastAbilityCommand { get; set; }
    }
}
