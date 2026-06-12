using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public sealed class CommandProcessor
    {
        private readonly TacticPauseValidator _tacticValidator = new();

        public IReadOnlyList<AvailableCommand> GetAvailableCommands(
            BoardState board,
            int requisition,
            int checkpointIndex)
        {
            var list = new List<AvailableCommand>();
            var usedAbilities = new HashSet<GrantedAbility>();

            foreach (var piece in board.Pieces)
            {
                var ability = piece.Definition.GrantedAbility;
                if (ability != GrantedAbility.None &&
                    !usedAbilities.Contains(ability) &&
                    CombatAbilityExecutor.CanUseAtPause(ability, checkpointIndex))
                {
                    usedAbilities.Add(ability);
                    list.Add(new AvailableCommand
                    {
                        Type = CommandType.UseAbility,
                        SourcePieceId = piece.InstanceId,
                        Ability = ability,
                        RequisitionCost = CombatAbilityExecutor.GetAuthorityCost(ability, checkpointIndex)
                    });
                }

                var actions = piece.Definition.CommandActions;
                if (actions == CommandActionFlags.None)
                    continue;

                if (actions.HasFlag(CommandActionFlags.SpendRequisitionBuff))
                {
                    list.Add(new AvailableCommand
                    {
                        Type = CommandType.SpendRequisitionBuff,
                        SourcePieceId = piece.InstanceId,
                        RequisitionCost = 1
                    });
                }

                if (actions.HasFlag(CommandActionFlags.CallStrike))
                {
                    list.Add(new AvailableCommand
                    {
                        Type = CommandType.CallStrike,
                        SourcePieceId = piece.InstanceId,
                        RequisitionCost = 2
                    });
                }
            }

            return list;
        }

        public int GetBonusActionSlots(BoardState board) => 0;

        public CommandResult TryApplyBatch(
            IReadOnlyList<PhaseCommand> commands,
            BoardState board,
            ref int authority,
            TacticState tactics,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            int checkpointIndex,
            int globalTick)
        {
            int logSegment = checkpointIndex + 1;
            int authoritySnapshot = authority;
            var tacticCommand = commands?.FirstOrDefault(c =>
                c.Type == CommandType.SetTactic || c.Type == CommandType.ChangeStance);
            if (tacticCommand != null)
            {
                bool hqAlive = playerCombatants.Any(c => c.HasTag(GameTagIds.Hq) && c.IsAlive);
                bool hasCommand = board.Pieces.Any(p =>
                    p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance));
                var previous = tactics.PlayerTactic;

                if (!_tacticValidator.CanContinue(
                        tacticCommand.Tactic,
                        previous,
                        hqAlive,
                        hasCommand,
                        checkpointIndex,
                        ref authority,
                        out var reason))
                {
                    authority = authoritySnapshot;
                    return CommandResult.Fail(reason);
                }

                tactics.PlayerTactic = tacticCommand.Tactic;
                log.Append(logSegment, globalTick, "tactic", "tactic_set", null, (int)tacticCommand.Tactic);
            }

            var usedAbilities = new HashSet<GrantedAbility>();
            foreach (var command in commands.Where(c => c.Type == CommandType.UseAbility))
            {
                if (usedAbilities.Contains(command.Ability))
                    return CommandResult.Fail("Duplicate ability");

                usedAbilities.Add(command.Ability);
                int cost = CombatAbilityExecutor.GetAuthorityCost(command.Ability, checkpointIndex);
                if (authority < cost)
                {
                    authority = authoritySnapshot;
                    return CommandResult.Fail("Insufficient Authority");
                }

                var result = CombatAbilityExecutor.Execute(
                    command.Ability,
                    command.SourcePieceId,
                    board,
                    playerCombatants,
                    enemyCombatants,
                    log,
                    logSegment,
                    globalTick,
                    command.TargetCell);
                if (!result.Success)
                {
                    authority = authoritySnapshot;
                    return result;
                }

                authority -= cost;
            }

            foreach (var command in commands.Where(c =>
                         c.Type is CommandType.SpendRequisitionBuff or CommandType.CallStrike))
            {
                var legacy = TryApplyLegacy(
                    command,
                    board,
                    ref authority,
                    tactics,
                    playerCombatants,
                    enemyCombatants,
                    log,
                    logSegment,
                    globalTick);
                if (!legacy.Success)
                {
                    authority = authoritySnapshot;
                    return legacy;
                }
            }

            return CommandResult.Ok();
        }

        public CommandResult TryApply(
            PhaseCommand command,
            BoardState board,
            ref int requisition,
            TacticState tactics,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            int checkpointIndex,
            int globalTick)
        {
            if (command.Type is CommandType.SpendRequisitionBuff or CommandType.CallStrike)
            {
                return TryApplyLegacy(
                    command,
                    board,
                    ref requisition,
                    tactics,
                    playerCombatants,
                    enemyCombatants,
                    log,
                    checkpointIndex + 1,
                    globalTick);
            }

            return TryApplyBatch(
                new[] { command },
                board,
                ref requisition,
                tactics,
                playerCombatants,
                enemyCombatants,
                log,
                checkpointIndex,
                globalTick);
        }

        private CommandResult TryApplyLegacy(
            PhaseCommand command,
            BoardState board,
            ref int requisition,
            TacticState tactics,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            int logSegment,
            int logTick)
        {
            var source = board.Pieces.FirstOrDefault(p => p.InstanceId == command.SourcePieceId);
            if (source == null)
                return CommandResult.Fail("Source piece not found");

            switch (command.Type)
            {
                case CommandType.SpendRequisitionBuff:
                    if (!source.Definition.CommandActions.HasFlag(CommandActionFlags.SpendRequisitionBuff))
                        return CommandResult.Fail("Piece cannot spend requisition");
                    if (requisition < command.Cost)
                        return CommandResult.Fail("Insufficient requisition");
                    requisition -= command.Cost;
                    tactics.PlayerDamageBuff += 2;
                    log.Append(logSegment, logTick, source.InstanceId, "requisition_buff", null, 2);
                    return CommandResult.Ok();

                case CommandType.CallStrike:
                    if (!source.Definition.CommandActions.HasFlag(CommandActionFlags.CallStrike))
                        return CommandResult.Fail("Piece cannot call strike");
                    if (requisition < command.Cost)
                        return CommandResult.Fail("Insufficient requisition");
                    requisition -= command.Cost;
                    ApplyStrikeDamage(playerCombatants, enemyCombatants, log, logSegment, logTick, source.InstanceId, damage: 5);
                    return CommandResult.Ok();

                default:
                    return CommandResult.Fail("Unknown command");
            }
        }

        private static void ApplyStrikeDamage(
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            int logSegment,
            int logTick,
            string actorId,
            int damage)
        {
            var target = enemyCombatants.Where(c => c.IsAlive).OrderBy(c => c.CurrentHp).FirstOrDefault();
            if (target == null)
                return;

            target.CurrentHp -= damage;
            log.Append(logSegment, logTick, actorId, "call_strike", target.InstanceId, damage);
            if (!target.IsAlive)
                log.Append(logSegment, logTick, target.InstanceId, "destroyed", actorId, 0);
        }
    }
}
