using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>Shared boards and commands for vertical-slice regression tests.</summary>
    public static class VerticalSliceTestFixtures
    {
        public const int RegressionRunSeed = 24_680;
        public const int RegressionRequisition = 8;

        public static BoardState BuildGauntletBoard(ContentDatabase database)
        {
            var faction = database.GetFaction("iron_vanguard");
            Assert.NotNull(faction, "iron_vanguard faction required for regression tests.");

            var board = new BoardState(faction.CreateBoardLayout());
            Place(board, database, "hq_command", new GridCoord(0, 4), "hq_player");
            Place(board, database, "radio_array", new GridCoord(2, 4), "radio_1");
            Place(board, database, "mobile_cannon", new GridCoord(4, 0), "cannon_1");
            Place(board, database, "grenade_thrower", new GridCoord(6, 2), "grenade_1");
            Place(board, database, "armored_transport", new GridCoord(4, 3), "transport_1");
            Place(board, database, "field_medic", new GridCoord(5, 6), "medic_1");
            Place(board, database, "conscript_rifleman", new GridCoord(6, 5), "conscript_1");
            Place(board, database, "diesel_walker", new GridCoord(7, 3), "walker_1");
            Place(board, database, "rifle_squad", new GridCoord(7, 6), "rifle_1");
            return board;
        }

        public static List<PhaseCommand> BuildAggressiveCommands(BoardState board)
        {
            var commands = new List<PhaseCommand>();
            string radioId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "radio_array")?.InstanceId;

            commands.Add(new PhaseCommand
            {
                AfterPhase = CombatPhase.Deployment,
                Type = CommandType.SetTactic,
                Tactic = TacticType.Advance,
                SourcePieceId = radioId ?? "player_tactic"
            });
            commands.Add(new PhaseCommand
            {
                AfterPhase = CombatPhase.Grind,
                Type = CommandType.SetTactic,
                Tactic = TacticType.Advance,
                SourcePieceId = radioId ?? "player_tactic"
            });

            TryAddAbility(commands, board, "grenade_thrower", GrantedAbility.GrenadeLob, CombatPhase.Deployment);
            TryAddAbility(commands, board, "armored_transport", GrantedAbility.ShieldAllies, CombatPhase.Deployment);
            TryAddAbility(commands, board, "mobile_cannon", GrantedAbility.CannonBlast, CombatPhase.Grind);

            return commands;
        }

        private static void TryAddAbility(
            List<PhaseCommand> commands,
            BoardState board,
            string pieceId,
            GrantedAbility ability,
            CombatPhase phase)
        {
            var piece = board.Pieces.FirstOrDefault(p => p.Definition.Id == pieceId);
            if (piece == null)
                return;

            commands.Add(new PhaseCommand
            {
                AfterPhase = phase,
                Type = CommandType.UseAbility,
                Ability = ability,
                SourcePieceId = piece.InstanceId
            });
        }

        private static void Place(
            BoardState board,
            ContentDatabase database,
            string pieceId,
            GridCoord anchor,
            string instanceId)
        {
            var piece = database.Pieces.First(p => p.id == pieceId).ToCore();
            var result = board.TryPlace(piece, anchor, instanceId);
            Assert.IsTrue(result.Success, $"Failed to place {pieceId} at {anchor}: {result.Reason}");
        }
    }
}
