using System.Linq;
using System.Text;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>Documents first-shop board strength bands vs fight 1 (heuristic ArmyStrengthCalculator).</summary>
    public sealed class FirstBoardStrengthBenchmarkTests
    {
        private ContentDatabase _database;
        private ContentRegistry _registry;
        private FactionSO _faction;
        private ArmyStrengthSnapshot _fight1;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            _registry = _database.BuildRegistry();
            _faction = _database.GetFaction(FactionIds.IronmarchUnion);
            var fight1Board = _database.GetEnemyTemplate(1).BuildBoard(_faction, _registry);
            _fight1 = ArmyStrengthCalculator.Evaluate(fight1Board);
        }

        [Test]
        public void Benchmark_FirstShopStrengthBands_PrintsReport()
        {
            var log = new StringBuilder();
            log.AppendLine("=== First Board Strength Benchmark (IronMarch Union) ===");
            log.AppendLine("Start: 50 supplies, 16 manpower after first muster (+1), 2 authority");
            log.AppendLine();
            log.AppendLine("Per-piece combat rating (fielding pieces only):");
            foreach (var pieceSo in _database.Pieces
                         .Select(p => p.ToCore())
                         .Where(ManpowerCalculator.CountsTowardFielding)
                         .OrderBy(PieceCombatRating.ComputeBase))
            {
                log.AppendLine(
                    $"  {pieceSo.Id,-24} {PieceCombatRating.ComputeBase(pieceSo),4}  ({pieceSo.GoldCost}G" +
                    (pieceSo.RequisitionCost > 0 ? $" +{pieceSo.RequisitionCost}A" : string.Empty) +
                    $", {pieceSo.ManpowerCost}MP)");
            }

            log.AppendLine();
            log.AppendLine($"Fight 1 enemy: base={_fight1.BaseTotal}, effective={_fight1.EffectiveTotal}");

            log.AppendLine();
            log.AppendLine("Scenario boards (combat fielding only):");
            LogScenario(log, "Empty", BuildCombat());
            LogScenario(log, "1× conscript (12G, 1MP)", BuildCombat(("conscript_rifleman", 0, 0)));
            LogScenario(log, "2× conscript (24G, 2MP)", BuildCombat(
                ("conscript_rifleman", 0, 0),
                ("conscript_rifleman", 1, 0)));
            LogScenario(log, "3× conscript (36G, 3MP)", BuildCombat(
                ("conscript_rifleman", 0, 0),
                ("conscript_rifleman", 1, 0),
                ("conscript_rifleman", 2, 0)));
            LogScenario(log, "4× conscript (48G, 4MP)", BuildCombat(
                ("conscript_rifleman", 0, 0),
                ("conscript_rifleman", 1, 0),
                ("conscript_rifleman", 2, 0),
                ("conscript_rifleman", 3, 0)));
            LogScenario(log, "2× conscript + medic (46G, 3MP)", BuildCombat(
                ("conscript_rifleman", 0, 0),
                ("conscript_rifleman", 1, 0),
                ("field_medic", 2, 0)));
            LogScenario(log, "enlisted + bulwark + conscript (45G, 3MP)", BuildCombat(
                ("enlisted_rifleman", 0, 0),
                ("bulwark_squad", 1, 0),
                ("conscript_rifleman", 2, 0)));
            LogScenario(log, "iron horse + 2× conscript (48G, 6MP)", BuildCombat(
                ("ironmarch_iron_horse", 0, 0),
                ("conscript_rifleman", 3, 0),
                ("conscript_rifleman", 4, 0)));
            LogScenario(log, "MG nest + 2× conscript (44G, 4MP)", BuildCombat(
                ("machine_gun_nest", 0, 0),
                ("conscript_rifleman", 2, 0),
                ("conscript_rifleman", 3, 0)));

            TestContext.WriteLine(log.ToString());
            Assert.Greater(_fight1.EffectiveTotal, 0);
        }

        private void LogScenario(StringBuilder log, string label, BoardState combat)
        {
            var snapshot = ArmyStrengthCalculator.Evaluate(combat);
            var assessment = MatchupAssessment.Compare(snapshot, _fight1);
            log.AppendLine(
                $"  {label,-42} eff={snapshot.EffectiveTotal,4}  vs F1={assessment.Ratio:0.00}  {MatchupAssessment.FormatLabel(assessment.Label)}");
        }

        private BoardState BuildCombat(params (string id, int x, int y)[] placements)
        {
            var combat = new BoardState(_faction.CreateCombatBoardLayout());
            for (int i = 0; i < placements.Length; i++)
            {
                var (id, x, y) = placements[i];
                var piece = _registry.GetById(id);
                Assert.IsTrue(combat.TryPlace(piece, new GridCoord(x, y), $"{id}_{i}").Success);
            }

            return combat;
        }
    }
}
