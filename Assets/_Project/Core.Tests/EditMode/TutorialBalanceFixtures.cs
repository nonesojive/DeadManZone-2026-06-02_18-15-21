using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public static class TutorialBalanceFixtures
    {
        public const int SeedSweepCount = 40;
        public const float MinPauseTwoReachRate = 0.90f;

        public static BoardState BuildReferencePlayerBoard(ContentDatabase database)
        {
            var faction = database.GetFaction("iron_vanguard");
            Assert.NotNull(faction);

            var board = new BoardState(faction.CreateBoardLayout());
            var hq = GetPiece(database, "hq_command");
            var conscript = GetPiece(database, "conscript_rifleman");
            Assert.NotNull(hq);
            Assert.NotNull(conscript);

            Assert.IsTrue(board.TryPlace(hq, new GridCoord(0, 4), "hq_player").Success);
            Assert.IsTrue(board.TryPlace(conscript, new GridCoord(5, 4), "conscript_1").Success);
            Assert.IsTrue(board.TryPlace(conscript, new GridCoord(5, 6), "conscript_2").Success);
            return board;
        }

        public static BoardState BuildEnemyBoard(ContentDatabase database, int fightIndex)
        {
            var faction = database.GetFaction("iron_vanguard");
            var template = database.GetEnemyTemplate(fightIndex);
            Assert.NotNull(template);
            return template.BuildBoard(faction, database.BuildRegistry());
        }

        public static bool ReachesPauseTwo(BoardState player, BoardState enemy, int seed)
        {
            var run = TickCombatRun.Start(player, enemy, seed, authority: 0);

            run.Continue(new List<PhaseCommand>());
            if (run.IsFightOver)
                return false;

            var deploymentCommands = new List<PhaseCommand>
            {
                new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.DisciplinedFire,
                    SourcePieceId = "player_tactic"
                }
            };

            run.Continue(deploymentCommands);
            if (run.LastCompletedPhase != CombatPhase.Grind)
                return false;

            // Spec allows early grind victory; only losses/draws fail reach.
            return run.AwaitingCommand
                   || (run.IsFightOver && run.PlayerWon && !run.IsDraw);
        }

        public static float MeasurePauseTwoReachRate(int fightIndex, ContentDatabase database, int seedBase = 5000)
        {
            var player = BuildReferencePlayerBoard(database);
            var enemy = BuildEnemyBoard(database, fightIndex);
            int pass = 0;

            for (int i = 0; i < SeedSweepCount; i++)
            {
                if (ReachesPauseTwo(player, enemy, seedBase + i))
                    pass++;
            }

            return pass / (float)SeedSweepCount;
        }

        private static PieceDefinition GetPiece(ContentDatabase database, string pieceId) =>
            database.Pieces.First(p => p.id == pieceId).ToCore();
    }
}
