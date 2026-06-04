namespace DeadManZone.Core.Combat
{
    public enum CombatPhase
    {
        Deployment = 1,
        Grind = 2,
        FinalPush = 3
    }

    public sealed class CombatEvent
    {
        public CombatPhase Phase { get; init; }
        public int Tick { get; init; }
        public string ActorId { get; init; }
        public string ActionType { get; init; }
        public string TargetId { get; init; }
        public int Value { get; init; }
    }

    public sealed class CombatEventLog
    {
        public System.Collections.Generic.List<CombatEvent> Events { get; } = new();

        public void Append(
            CombatPhase phase,
            int tick,
            string actorId,
            string actionType,
            string targetId,
            int value) =>
            Events.Add(new CombatEvent
            {
                Phase = phase,
                Tick = tick,
                ActorId = actorId,
                ActionType = actionType,
                TargetId = targetId,
                Value = value
            });
    }

    public sealed class CombatResult
    {
        public CombatEventLog EventLog { get; init; }
        public bool PlayerWon { get; init; }
    }
}
