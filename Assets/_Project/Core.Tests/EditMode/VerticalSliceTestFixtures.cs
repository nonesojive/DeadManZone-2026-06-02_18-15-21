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
            Place(board, database, "field_gun_nest", new GridCoord(0, 0), "gun_1");
            Place(board, database, "supply_depot", new GridCoord(2, 0), "depot_1");
            Place(board, database, "command_bunker", new GridCoord(1, 4), "bunker_1");
            Place(board, database, "mortar_crew", new GridCoord(4, 3), "mortar_1");
            Place(board, database, "mg_team", new GridCoord(5, 4), "mg_1");
            Place(board, database, "mobile_artillery", new GridCoord(6, 3), "artillery_1");
            Place(board, database, "diesel_walker", new GridCoord(7, 4), "walker_1");
            // Walker is 2x2 at (7,4) -> through (8,5); rifle sits above that block at (8,3).
            Place(board, database, "rifle_squad", new GridCoord(8, 3), "rifle_1");
            return board;
        }

        public static List<PhaseCommand> BuildAggressiveCommands(BoardState board)
        {
            var commands = new List<PhaseCommand>();
            string bunkerId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "command_bunker")?.InstanceId;
            string depotId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "supply_depot")?.InstanceId;

            if (bunkerId != null)
            {
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.ChangeStance,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = bunkerId
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.ChangeStance,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = bunkerId
                });
            }

            if (depotId != null)
            {
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.SpendRequisitionBuff,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = depotId,
                    Cost = 1
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.SpendRequisitionBuff,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = depotId,
                    Cost = 1
                });
            }

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
