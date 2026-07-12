using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// The player's in-progress order sheet for one tactic pause: selected doctrine
    /// (tactic) + queued ability commands. Pure state + budget arithmetic — every
    /// accept/reject verdict comes from <see cref="TacticPauseValidator"/> (the Core
    /// authority); this class never re-implements a rule. Plain C# so it is EditMode
    /// testable without a scene.
    /// </summary>
    public sealed class CombatTacticOrdersDraft
    {
        public sealed class QueuedAbility
        {
            public AvailableCommand Command;
            public GridCoord? TargetCell;
        }

        private readonly TacticPauseValidator _validator = new();
        private readonly List<QueuedAbility> _queued = new();
        private readonly TacticType _activeTactic;
        private readonly bool _hasCommandPiece;
        private readonly int _checkpointIndex;
        private readonly int _authority;
        private readonly TacticType[] _startingTactics;

        public CombatTacticOrdersDraft(
            TacticType activeTactic,
            int authority,
            int checkpointIndex,
            bool hasCommandPiece,
            TacticType[] startingTactics = null)
        {
            _activeTactic = activeTactic;
            SelectedTactic = activeTactic;
            _authority = authority;
            _checkpointIndex = checkpointIndex;
            _hasCommandPiece = hasCommandPiece;
            _startingTactics = startingTactics;
        }

        public TacticType SelectedTactic { get; private set; }
        public TacticType ActiveTactic => _activeTactic;
        public int CheckpointIndex => _checkpointIndex;
        public int AuthorityTotal => _authority;
        public IReadOnlyList<QueuedAbility> Queued => _queued;

        public int TotalCost => TacticPauseValidator.GetTotalPauseCost(
            SelectedTactic, _activeTactic, _checkpointIndex, _queued.Select(q => q.Command.Ability));

        public int AuthorityRemaining => _authority - TotalCost;

        /// <summary>Whole sheet legal as-is? The RESUME gate.</summary>
        public bool Validate(out string reason) =>
            _validator.ValidatePause(
                SelectedTactic, _activeTactic, _hasCommandPiece, _checkpointIndex,
                _authority, _queued.Select(q => q.Command.Ability), out reason, _startingTactics);

        /// <summary>Switch doctrine; rejected (selection unchanged) if the switch makes the sheet illegal.</summary>
        public bool TrySelectTactic(TacticType tactic, out string reason)
        {
            if (!_validator.ValidatePause(
                    tactic, _activeTactic, _hasCommandPiece, _checkpointIndex,
                    _authority, _queued.Select(q => q.Command.Ability), out reason, _startingTactics))
                return false;

            SelectedTactic = tactic;
            return true;
        }

        /// <summary>Queue an ability command; rejected if duplicate or over budget.</summary>
        public bool TryQueueAbility(AvailableCommand command, GridCoord? targetCell, out string reason)
        {
            if (command == null || command.Type != CommandType.UseAbility)
            {
                reason = "Not an ability command";
                return false;
            }

            if (_queued.Any(q => q.Command.Ability == command.Ability))
            {
                reason = "Ability already queued";
                return false;
            }

            var candidate = _queued.Select(q => q.Command.Ability).Append(command.Ability);
            if (!_validator.ValidatePause(
                    SelectedTactic, _activeTactic, _hasCommandPiece, _checkpointIndex,
                    _authority, candidate, out reason, _startingTactics))
                return false;

            _queued.Add(new QueuedAbility { Command = command, TargetCell = targetCell });
            return true;
        }

        public void RemoveQueuedAt(int index)
        {
            if (index >= 0 && index < _queued.Count)
                _queued.RemoveAt(index);
        }

        public bool IsAbilityQueued(GrantedAbility ability) =>
            _queued.Any(q => q.Command.Ability == ability);

        /// <summary>The exact PhaseCommands the real flow submits: SetTactic first,
        /// then the queued abilities.</summary>
        public List<PhaseCommand> BuildCommands()
        {
            var commands = new List<PhaseCommand>
            {
                new PhaseCommand
                {
                    AfterCheckpoint = _checkpointIndex,
                    Type = CommandType.SetTactic,
                    Tactic = SelectedTactic,
                    SourcePieceId = "player_tactic"
                }
            };

            foreach (var queued in _queued)
            {
                commands.Add(new PhaseCommand
                {
                    AfterCheckpoint = _checkpointIndex,
                    Type = CommandType.UseAbility,
                    Ability = queued.Command.Ability,
                    SourcePieceId = queued.Command.SourcePieceId,
                    Cost = queued.Command.RequisitionCost,
                    TargetCell = queued.TargetCell
                });
            }

            return commands;
        }
    }
}
