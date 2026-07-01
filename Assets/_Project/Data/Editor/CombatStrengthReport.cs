using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class CombatStrengthReport
    {
        [MenuItem(DeadManZoneEditorMenus.Content + "Combat Strength Report")]
        public static void Run()
        {
            var database = ContentDatabase.Load();
            var faction = database.GetFaction(FactionIds.IronVanguard);
            var registry = database.BuildRegistry();
            var referencePlayer = BuildReferencePlayerBoard(database, faction);
            var referenceSnapshot = ArmyStrengthCalculator.Evaluate(referencePlayer);

            var log = new System.Text.StringBuilder();
            log.AppendLine("[Combat Strength Report]");
            log.AppendLine(
                $"Reference player: base={referenceSnapshot.BaseTotal}, effective={referenceSnapshot.EffectiveTotal}");
            log.AppendLine("Fight | Enemy Base | Enemy Eff | vs Ref Ratio | Label");
            log.AppendLine("------|------------|-----------|--------------|------");

            for (int fight = 1; fight <= 10; fight++)
            {
                var template = database.GetEnemyTemplate(fight);
                var enemyBoard = template.BuildBoard(faction, registry);
                var enemy = ArmyStrengthCalculator.Evaluate(enemyBoard);
                var assessment = MatchupAssessment.Compare(referenceSnapshot, enemy);

                log.AppendLine(
                    $"{fight,5} | {enemy.BaseTotal,10} | {enemy.EffectiveTotal,9} | {assessment.Ratio,12:0.00} | {MatchupAssessment.FormatLabel(assessment.Label)}");
            }

            Debug.Log(log.ToString());
        }

        private static BoardState BuildReferencePlayerBoard(ContentDatabase database, FactionSO faction)
        {
            var board = new BoardState(faction.CreateBoardLayout());
            var hq = database.Pieces.FirstOrDefault(p => p.id == "ironmarch_hq")?.ToCore();
            var conscript = database.Pieces.FirstOrDefault(p => p.id == "conscript_rifleman")?.ToCore();
            var rifle = database.Pieces.FirstOrDefault(p => p.id == "rifle_squad")?.ToCore();
            if (hq != null)
                board.TryPlace(hq, new GridCoord(0, 4), "hq_player");
            if (conscript != null)
            {
                board.TryPlace(conscript, new GridCoord(faction.rearCols + 1, 4), "conscript_1");
                board.TryPlace(conscript, new GridCoord(faction.rearCols + 1, 6), "conscript_2");
            }

            if (rifle != null)
                board.TryPlace(rifle, new GridCoord(faction.rearCols + faction.supportCols, 4), "rifle_1");

            return board;
        }
    }
}
