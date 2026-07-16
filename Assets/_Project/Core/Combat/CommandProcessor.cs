using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

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
            int artilleryCount = 0,
            IDictionary<string, GridCoord> transportTargetSink = null)
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

            // Doctor Recursion (2026-07-15 faction-roster-v1 §1.8/§2.6): "your pause-window
            // abilities each fire twice" — deterministic, zero randomness, no extra Authority
            // cost. Army-wide: any active piece carrying the flag switches it on for the batch.
            bool repeatAbilities = playerCombatants.Any(c => c.IsActive && c.Definition.RepeatsPauseAbilities);

            var usedAbilities = new HashSet<GrantedAbility>();
            foreach (var command in commands.Where(c => c.Type == CommandType.UseAbility))
            {
                if (usedAbilities.Contains(command.Ability))
                    return CommandResult.Fail("Duplicate ability");

                usedAbilities.Add(command.Ability);

                if (command.Ability == GrantedAbility.Echo)
                {
                    // Resonance Coil's Echo (§2.6): repeat the last ability issued this fight,
                    // free. Border rule (§1.8): Paradox manipulates only its own tempo — Echo
                    // replays an ability, never a SetTactic, and isn't itself echoable.
                    bool sourceGrantsEcho =
                        board.Pieces.Any(p => p.InstanceId == command.SourcePieceId && p.Definition.GrantedAbility == GrantedAbility.Echo)
                        || (hqBoard != null && hqBoard.Pieces.Any(p => p.InstanceId == command.SourcePieceId && p.Definition.GrantedAbility == GrantedAbility.Echo));
                    if (!sourceGrantsEcho)
                    {
                        authority = authoritySnapshot;
                        return CommandResult.Fail("Source cannot grant Echo");
                    }

                    var echoed = tactics.LastAbilityCommand;
                    if (echoed == null)
                    {
                        authority = authoritySnapshot;
                        return CommandResult.Fail("No prior ability to echo");
                    }

                    var echoResult = CombatAbilityExecutor.Execute(
                        echoed.Ability,
                        echoed.SourcePieceId,
                        board,
                        playerCombatants,
                        enemyCombatants,
                        log,
                        logSegment,
                        globalTick,
                        echoed.TargetCell,
                        hqBoard,
                        artilleryCount);
                    if (!echoResult.Success)
                    {
                        authority = authoritySnapshot;
                        return echoResult;
                    }

                    log.Append(logSegment, globalTick, command.SourcePieceId, "echo", echoed.SourcePieceId, (int)echoed.Ability);

                    if (repeatAbilities)
                        CombatAbilityExecutor.Execute(
                            echoed.Ability,
                            echoed.SourcePieceId,
                            board,
                            playerCombatants,
                            enemyCombatants,
                            log,
                            logSegment,
                            globalTick,
                            echoed.TargetCell,
                            hqBoard,
                            artilleryCount);

                    continue;
                }

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
                tactics.LastAbilityCommand = command;

                if (repeatAbilities)
                    CombatAbilityExecutor.Execute(
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
            }

            foreach (var command in commands.Where(c => c.Type == CommandType.TransportTarget))
            {
                // §2.5 Armored Ark: "at the opening pause window the player targets a cell" —
                // choice of WHERE, not when. No other window may (re)target a transport.
                if (checkpointIndex != 0)
                    return CommandResult.Fail("Transport targeting is opening-window only");

                if (transportTargetSink == null || !command.TargetCell.HasValue)
                    return CommandResult.Fail("Transport target cell required");

                var transport = playerCombatants.FirstOrDefault(c =>
                    c.InstanceId == command.SourcePieceId && c.IsTransport && c.IsActive);
                if (transport == null)
                    return CommandResult.Fail("Transport not found");

                transportTargetSink[transport.InstanceId] = command.TargetCell.Value;
                log.Append(
                    logSegment,
                    globalTick,
                    transport.InstanceId,
                    "transport_target",
                    $"{command.TargetCell.Value.X},{command.TargetCell.Value.Y}",
                    0);
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
