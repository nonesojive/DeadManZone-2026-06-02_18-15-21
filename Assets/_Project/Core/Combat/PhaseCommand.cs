namespace DeadManZone.Core.Combat
{
    public enum CommandType
    {
        ChangeStance,
        SpendRequisitionBuff,
        CallStrike
    }

    public sealed class PhaseCommand
    {
        public CombatPhase AfterPhase { get; set; }
        public CommandType Type { get; set; }
        public StanceType Stance { get; set; }
        public int Cost { get; set; }
        public string SourcePieceId { get; set; }
    }

    public sealed class AvailableCommand
    {
        public CommandType Type { get; init; }
        public string SourcePieceId { get; init; }
        public int RequisitionCost { get; init; }
    }

    public readonly struct CommandResult
    {
        public bool Success { get; init; }
        public string Reason { get; init; }

        public static CommandResult Ok() => new CommandResult { Success = true };

        public static CommandResult Fail(string reason) =>
            new CommandResult { Success = false, Reason = reason };
    }
}
