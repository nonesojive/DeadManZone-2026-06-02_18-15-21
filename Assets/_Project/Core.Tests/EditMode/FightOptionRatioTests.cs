using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>PROVISIONAL 2026-07-19 owner spec (fight-option strength ratios): the three
    /// fronts' DISPLAYED strengths sit in deliberate ratios of the Normal draw — Easy 0.85x,
    /// Hard 1.30x (±5%) — enforced by BoardStrengthScaler solving a uniform per-piece
    /// StatScale. Sweeps the shipped ContentDatabase across seeds and dread levels, plus
    /// database-free solver unit checks (monotonicity + convergence) on a hand-built board.</summary>
    public sealed class FightOptionRatioTests
    {
        private ContentDatabase _database;
        private DeadManZone.Core.Content.ContentRegistry _registry;
        private FactionSO _faction;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
        }

        private void RequireDatabase()
        {
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
        }

        private List<FightOptionArmySource> BuildSources()
        {
            _registry = _database.BuildRegistry();
            _faction = _database.GetFaction(FactionIds.IronmarchUnion);
            Assert.NotNull(_faction, "IronMarch faction must exist in the database");
            var registry = _registry;
            var faction = _faction;
            return _database.EnemyTemplates
                .Where(t => t != null)
                .Select(t => new FightOptionArmySource
                {
                    FightNumber = t.fightNumber,
                    EnemyFactionId = t.enemyFactionId,
                    BuildBoard = () => t.BuildBoard(faction, registry)
                })
                .ToList();
        }

        [Test]
        public void Generate_DisplayedStrengths_SitAtTheTierRatios()
        {
            RequireDatabase();
            var sources = BuildSources();

            for (int seed = 1; seed <= 6; seed++)
            foreach (int dread in new[] { 0, 4, 8 })
            {
                int round = 1 + dread / 4;
                var options = FightOptionGenerator.Generate(seed, round, dread, sources);
                Assert.AreEqual(3, options.Count);

                var easy = options.Single(o => o.Tier == FightOptionTier.Easy);
                var normal = options.Single(o => o.Tier == FightOptionTier.Normal);
                var hard = options.Single(o => o.Tier == FightOptionTier.Hard);
                string ctx = $"seed {seed} round {round} dread {dread}: " +
                    $"E {easy.StrengthPreview} / N {normal.StrengthPreview} / H {hard.StrengthPreview}";

                Assert.Greater(normal.StrengthPreview, 0, ctx);
                float easyRatio = easy.StrengthPreview / (float)normal.StrengthPreview;
                float hardRatio = hard.StrengthPreview / (float)normal.StrengthPreview;

                Assert.LessOrEqual(
                    System.Math.Abs(easyRatio - FightOptionGenerator.EasyStrengthRatio), 0.05f,
                    $"{ctx} — easy/normal {easyRatio:F3} must sit within ±0.05 of 0.85");
                Assert.LessOrEqual(
                    System.Math.Abs(hardRatio - FightOptionGenerator.HardStrengthRatio), 0.065f,
                    $"{ctx} — hard/normal {hardRatio:F3} must sit within ±0.065 of 1.30");

                foreach (var option in options)
                {
                    Assert.That(option.StatScale,
                        Is.InRange(BoardStrengthScaler.MinScale, BoardStrengthScaler.MaxScale),
                        $"{ctx} — {option.Tier} StatScale {option.StatScale}");
                }

                // PROVISIONAL 2026-07-19 deep balance pass: Normal's board is untouched by
                // ratio scaling, but its record carries the ABSOLUTE scale BuildBoard's
                // ladder normalization (EnemyLadder) left on the board — ApplyScale SETS
                // StatScale at fight time, so the recorded scale must round-trip the exact
                // army the report rated. Verify via the orchestrator's rebuild path.
                var normalTemplate = _database.GetEnemyTemplate(
                    normal.TemplateFightNumber, normal.EnemyFactionId);
                Assert.NotNull(normalTemplate, $"{ctx} — Normal's template must resolve");
                var rebuilt = normalTemplate.BuildBoard(_faction, _registry);
                BoardStrengthScaler.ApplyScale(rebuilt, normal.StatScale);
                int rebuiltRating = ArmyStrengthCalculator.Evaluate(rebuilt).EffectiveTotal;
                Assert.AreEqual(normal.StrengthPreview, rebuiltRating,
                    $"{ctx} — rebuilding Normal at its recorded StatScale must field the rated army");
            }
        }

        // ---- BoardStrengthScaler unit checks (no ContentDatabase) ----

        private static BoardState BoardWithRifles(int count)
        {
            var board = new BoardState(TestBoards.CombatLayout);
            int width = TestBoards.CombatBoardSize;
            for (int i = 0; i < count; i++)
                Assert.IsTrue(
                    board.TryPlace(TestPieces.RifleSquad(), new GridCoord(i % width, i / width), $"enemy_{i}").Success,
                    $"rifle {i} must place");
            return board;
        }

        [Test]
        public void SolveScale_RatingIsMonotoneInScale()
        {
            var board = BoardWithRifles(8);
            int previous = 0;
            foreach (float scale in new[] { 0.4f, 0.8f, 1.2f, 1.6f, 2.0f, 2.5f })
            {
                BoardStrengthScaler.ApplyScale(board, scale);
                int rating = ArmyStrengthCalculator.Evaluate(board).EffectiveTotal;
                Assert.GreaterOrEqual(rating, previous,
                    $"rating must not decrease as the scale rises (scale {scale})");
                previous = rating;
            }

            BoardStrengthScaler.ApplyScale(board, 0.4f);
            int low = ArmyStrengthCalculator.Evaluate(board).EffectiveTotal;
            Assert.Greater(previous, low, "the rating must genuinely move across [0.4, 2.5]");
        }

        [Test]
        public void SolveScale_ConvergesOnReachableTargets()
        {
            var board = BoardWithRifles(8);
            int baseline = ArmyStrengthCalculator.Evaluate(board).EffectiveTotal;

            foreach (float ratio in new[] { 0.85f, 1.30f })
            {
                int target = (int)System.Math.Round(ratio * baseline);
                float solved = BoardStrengthScaler.SolveScale(BoardWithRifles(8), target);
                Assert.That(solved,
                    Is.InRange(BoardStrengthScaler.MinScale, BoardStrengthScaler.MaxScale));

                var check = BoardWithRifles(8);
                BoardStrengthScaler.ApplyScale(check, solved);
                int achieved = ArmyStrengthCalculator.Evaluate(check).EffectiveTotal;
                Assert.LessOrEqual(System.Math.Abs(achieved - target) / (float)target, 0.02f,
                    $"target {target} (ratio {ratio}): achieved {achieved} at scale {solved} " +
                    "must land within 2% (integer per-piece rounding bounds the residual)");
            }
        }

        [Test]
        public void SolveScale_LeavesTheBoardAtTheSolvedScale_AndHandlesEmptyBoards()
        {
            var board = BoardWithRifles(4);
            int target = (int)System.Math.Round(
                1.3f * ArmyStrengthCalculator.Evaluate(board).EffectiveTotal);
            float solved = BoardStrengthScaler.SolveScale(board, target);

            Assert.IsTrue(board.Pieces.All(p => p.StatScale == solved),
                "SolveScale must leave every piece at the returned scale");

            Assert.AreEqual(1f, BoardStrengthScaler.SolveScale(null, 100));
            Assert.AreEqual(1f, BoardStrengthScaler.SolveScale(new BoardState(TestBoards.CombatLayout), 100));
            Assert.AreEqual(1f, BoardStrengthScaler.SolveScale(BoardWithRifles(1), 0),
                "a non-positive target must be a no-op");
        }
    }
}
