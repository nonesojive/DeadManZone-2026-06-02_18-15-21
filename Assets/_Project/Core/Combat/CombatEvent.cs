namespace DeadManZone.Core.Combat
{
    /// <summary>Why a combat pause fired: which checkpoint, which side crossed, at what threshold.</summary>
    public sealed class PauseTriggerContext
    {
        public int CheckpointIndex { get; init; }
        public CombatSide TriggeredBy { get; init; }
        public float Threshold { get; init; }
    }

    public sealed class CombatEvent
    {
        /// <summary>Playback segment: 0 = start→pause 1, 1 = pause 1→pause 2, 2 = remainder.</summary>
        public int Segment { get; init; }
        /// <summary>Global fight tick — never resets across segments.</summary>
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
            int segment,
            int tick,
            string actorId,
            string actionType,
            string targetId,
            int value) =>
            Events.Add(new CombatEvent
            {
                Segment = segment,
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
