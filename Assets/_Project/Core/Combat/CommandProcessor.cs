using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public sealed class CommandProcessor
    {
        public IReadOnlyList<AvailableCommand> GetAvailableCommands(
            BoardState board,
            int requisition,
            CombatPhase completedPhase)
        {
            var list = new List<AvailableCommand>();

            foreach (var piece in board.Pieces)
            {
                var actions = piece.Definition.CommandActions;
                if (actions == CommandActionFlags.None)
                    continue;

                if (actions.HasFlag(CommandActionFlags.ChangeStance))
                {
                    list.Add(new AvailableCommand
                    {
                        Type = CommandType.ChangeStance,
                        SourcePieceId = piece.InstanceId
                    });
                }

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

        public int GetBonusActionSlots(BoardState board)
        {
            bool commandOnSpecial = board.Pieces.Any(piece =>
                piece.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance) &&
                board.IsOnSpecialTile(piece.InstanceId));

            return commandOnSpecial ? 1 : 0;
        }

        public CommandResult TryApply(
            PhaseCommand command,
            BoardState board,
            ref int requisition,
            StanceState stances,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            CombatPhase completedPhase)
        {
            var source = board.Pieces.FirstOrDefault(p => p.InstanceId == command.SourcePieceId);
            if (source == null)
                return CommandResult.Fail("Source piece not found");

            switch (command.Type)
            {
                case CommandType.ChangeStance:
                    if (!source.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance))
                        return CommandResult.Fail("Piece cannot change stance");
                    stances.PlayerStance = command.Stance;
                    log.Append(completedPhase, tick: -1, source.InstanceId, "stance_change", null, (int)command.Stance);
                    return CommandResult.Ok();

                case CommandType.SpendRequisitionBuff:
                    if (!source.Definition.CommandActions.HasFlag(CommandActionFlags.SpendRequisitionBuff))
                        return CommandResult.Fail("Piece cannot spend requisition");
                    if (requisition < command.Cost)
                        return CommandResult.Fail("Insufficient requisition");
                    requisition -= command.Cost;
                    stances.PlayerDamageBuff += 2;
                    log.Append(completedPhase, tick: -1, source.InstanceId, "requisition_buff", null, 2);
                    return CommandResult.Ok();

                case CommandType.CallStrike:
                    if (!source.Definition.CommandActions.HasFlag(CommandActionFlags.CallStrike))
                        return CommandResult.Fail("Piece cannot call strike");
                    if (requisition < command.Cost)
                        return CommandResult.Fail("Insufficient requisition");
                    requisition -= command.Cost;
                    ApplyStrikeDamage(playerCombatants, enemyCombatants, log, completedPhase, source.InstanceId, damage: 5);
                    return CommandResult.Ok();

                default:
                    return CommandResult.Fail("Unknown command");
            }
        }

        private static void ApplyStrikeDamage(
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            CombatPhase phase,
            string actorId,
            int damage)
        {
            var target = enemyCombatants.Where(c => c.IsAlive).OrderBy(c => c.CurrentHp).FirstOrDefault();
            if (target == null)
                return;

            target.CurrentHp -= damage;
            log.Append(phase, tick: -1, actorId, "call_strike", target.InstanceId, damage);
        }
    }
}
