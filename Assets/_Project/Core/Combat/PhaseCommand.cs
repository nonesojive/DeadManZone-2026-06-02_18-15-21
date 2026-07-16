using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public enum CommandType
    {
        SetTactic,
        UseAbility,
        ChangeStance,
        SpendRequisitionBuff,
        CallStrike,
        /// <summary>2026-07-15 faction-roster-v1 §2.5 transport tentpole: opening-window-only
        /// command targeting a cell for a fielded transport (SourcePieceId) to drive to and
        /// unload at. Choice of WHERE, not when — CommandProcessor rejects it outside
        /// checkpoint 0. Free (no Authority cost), PROVISIONAL.</summary>
        TransportTarget
    }

    public sealed class PhaseCommand
    {
        /// <summary>Pause index this command applies at: 0 = first pause, 1 = second.</summary>
        public int AfterCheckpoint { get; set; }
        public CommandType Type { get; set; }
        public TacticType Tactic { get; set; }
        public GrantedAbility Ability { get; set; }
        public int Cost { get; set; }
        public string SourcePieceId { get; set; }
        public GridCoord? TargetCell { get; set; }

        // Legacy alias for older saves/tests.
        public TacticType Stance
        {
            get => Tactic;
            set => Tactic = value;
        }
    }

    public sealed class AvailableCommand
    {
        public CommandType Type { get; init; }
        public string SourcePieceId { get; init; }
        /// <summary>Human-readable source piece name for pause UI labels — run-flow
        /// instance ids are GUIDs and must never reach the screen.</summary>
        public string SourceDisplayName { get; init; }
        public int RequisitionCost { get; init; }
        public GrantedAbility Ability { get; init; }
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
