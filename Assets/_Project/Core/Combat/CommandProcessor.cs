using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public sealed class CommandProcessor
    {
        private readonly TacticPauseValidator _tacticValidator = new();

        /// <param name="hqBoard">2026-07-15 faction-roster-v1 §4 (🟡 ledger): HQ-board buildings
        /// (Artillery Park) can grant pause-window abilities too — scanned alongside the combat
        /// board so they show up as available commands.</param>
        public IReadOnlyList<AvailableCommand> GetAvailableCommands(
            BoardState board,
            int requisition,
            int checkpointIndex,
            BoardState hqBoard = null)
        {
            var list = new List<AvailableCommand>();
            var usedAbilities = new HashSet<GrantedAbility>();

            var pieces = hqBoard == null ? board.Pieces : board.Pieces.Concat(hqBoard.Pieces);
            foreach (var piece in pieces)
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
                        SourceDisplayName = piece.Definition.DisplayName,
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

        public CommandResult TryApplyBatch(
            IReadOnlyList<PhaseCommand> commands,
            BoardState board,
            ref int authority,
            TacticState tactics,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            int checkpointIndex,
            int logSegment,
            int globalTick,
            TacticType[] startingTactics = null,
            BoardState hqBoard = null,
            int artilleryCount = 0)
        {
            int authoritySnapshot = authority;
            var tacticCommand = commands?.FirstOrDefault(c =>
                c.Type == CommandType.SetTactic || c.Type == CommandType.ChangeStance);
            if (tacticCommand != null)
            {
                bool hasCommand = board.Pieces.Any(p =>
                    p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance));
                var previous = tactics.PlayerTactic;

                if (!_tacticValidator.CanContinue(
                        tacticCommand.Tactic,
                        previous,
                        hasCommand,
                        checkpointIndex,
                        ref authority,
                        out var reason,
                        startingTactics))
                {
                    authority = authoritySnapshot;
                    return CommandResult.Fail(reason);
                }

                tactics.PlayerTactic = tacticCommand.Tactic;
                // Keep the cached damage buff in sync with the live tactic, mirroring
                // TickCombatRun.SetPlayerTactic — otherwise mid-fight changes fight with
                // the old tactic's buff and save-restore diverges from the live fight.
                tactics.PlayerDamageBuff = TacticEffects.GetDamageBuff(tacticCommand.Tactic);
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
                    command.TargetCell,
                    hqBoard,
                    artilleryCount);
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
            // IsActive, not IsAlive (M5): a routed enemy has left the field — a strike
            // aimed at it would burn Authority on a unit already out of the fight.
            var target = enemyCombatants.Where(c => c.IsActive).OrderBy(c => c.CurrentHp).FirstOrDefault();
            if (target == null)
                return;

            target.CurrentHp -= damage;
            log.Append(logSegment, logTick, actorId, "call_strike", target.InstanceId, damage);
            if (!target.IsAlive)
                log.Append(logSegment, logTick, target.InstanceId, "destroyed", actorId, 0);
        }
    }
}
