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
            Place(board, database, "mobile_cannon", new GridCoord(0, 0), "cannon_1");
            Place(board, database, "field_medic", new GridCoord(2, 0), "medic_1");
            Place(board, database, "radio_array", new GridCoord(1, 4), "radio_1");
            Place(board, database, "grenade_thrower", new GridCoord(4, 3), "grenade_1");
            Place(board, database, "conscript_rifleman", new GridCoord(5, 4), "conscript_1");
            Place(board, database, "armored_transport", new GridCoord(3, 2), "transport_1");
            Place(board, database, "diesel_walker", new GridCoord(6, 4), "walker_1");
            Place(board, database, "rifle_squad", new GridCoord(8, 3), "rifle_1");
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

            return commands;
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
