namespace DeadManZone.Core.Combat
{
    public enum CombatAttackOutcomeKind
    {
        Hit,
        Graze,
        Miss
    }

    public sealed class CombatAttackOutcome
    {
        public CombatAttackOutcomeKind Kind { get; init; }
        public int Damage { get; init; }
        public int Roll { get; init; }
        public int EffectiveAccuracy { get; init; }
        public int GrazeBand { get; init; }
    }
}
