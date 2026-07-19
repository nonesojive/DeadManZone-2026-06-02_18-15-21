using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-12 balance-pass goldens: enemy-template strength curve,
    /// template synergy activation from fight 3 on, boss stage escalation after the
    /// stage-3 heat trim, and the softened death shock. Template assertions read the
    /// shipped ContentDatabase, so they reflect the authored curve only after the
    /// templates are regenerated (DeadManZone → Content → Regenerate Enemy Templates
    /// Only).</summary>
    public sealed class BalancePassTests
    {
        private ContentDatabase _database;
        private ContentRegistry _registry;
        private FactionSO _faction;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
                return; // content tests self-ignore via RequireDatabase; the golden below runs regardless

            _registry = _database.BuildRegistry();
            _faction = _database.GetFaction(FactionIds.IronmarchUnion);
        }

        /// <summary>Ladder-band tolerance: BuildBoard's normalization solves to 0.5%
        /// (BoardStrengthScaler.ConvergenceTolerance) but per-piece integer rounding and
        /// the [0.4, 2.5] scale clamps leave a residual; ±5% is the owner-spec band.</summary>
        private const float LadderBandTolerance = 0.05f;

        /// <summary>PROVISIONAL 2026-07-19 owner spec (deep balance pass): templates are
        /// ladder-NORMALIZED at build time (EnemyTemplateSO.BuildBoard → EnemyLadder), so
        /// the old non-decreasing golden is guaranteed-monotone and its assertion is now a
        /// band check against the canonical curve. This also permanently de-flakes the old
        /// test: it failed intermittently on 701-vs-704 near-ties (IronMarch fights 8/9)
        /// caused by critical-mass shared state; with 21% ladder gaps and ±5% bands that
        /// sensitivity is gone.</summary>
        [Test]
        public void EnemyTemplates_EffectiveTotal_TracksLadderAcrossFights1To10()
        {
            RequireDatabase();

            int previous = 0;
            for (int fight = 1; fight <= 10; fight++)
            {
                var template = _database.GetEnemyTemplate(fight);
                Assert.NotNull(template, $"missing enemy template for fight {fight}");
                Assert.AreEqual(fight, template.fightNumber, $"fight {fight} must have a dedicated template");

                var board = BuildTemplateBoard(template);
                int effective = ArmyStrengthCalculator.Evaluate(board).EffectiveTotal;
                int target = EnemyLadder.TargetStrength(fight);
                Assert.LessOrEqual(System.Math.Abs(effective - target) / (float)target, LadderBandTolerance,
                    $"fight {fight} (effective {effective}) must sit within ±5% of the ladder target {target}");

                // Bonus: monotonicity follows from the bands (21% gaps >> 2x5% band widths).
                Assert.Greater(effective, previous,
                    $"fight {fight} (effective {effective}) must outgun fight {fight - 1} (effective {previous})");
                previous = effective;
            }
        }

        /// <summary>Wave 5 (2026-07-17): each of the 8 factions authors its own 10-fight
        /// enemy ladder (DustScourgeEnemyFactory etc.) — this is the per-faction version of
        /// the golden above. Filters ContentDatabase.EnemyTemplates by enemyFactionId directly
        /// (not ContentDatabase.GetEnemyTemplate, which is deliberately faction-blind for
        /// fightNumber-only legacy callers) so each pool's own curve is verified in isolation.
        /// PROVISIONAL 2026-07-19 owner spec: normalization makes every faction's curve THE
        /// SAME deliberate curve — cross-faction template strength no longer varies wildly
        /// at the same fight index. Same band assertion (and the same de-flake) as above.</summary>
        [Test]
        public void EnemyTemplates_PerFaction_EffectiveTotal_TracksLadderAcrossFights1To10()
        {
            RequireDatabase();

            foreach (var factionId in FactionIds.Playable)
            {
                var faction = _database.GetFaction(factionId);
                Assert.NotNull(faction, $"missing FactionSO for '{factionId}'");

                var templatesByFight = _database.EnemyTemplates
                    .Where(t => t != null && t.enemyFactionId == factionId)
                    .ToDictionary(t => t.fightNumber);

                int previous = 0;
                for (int fight = 1; fight <= 10; fight++)
                {
                    Assert.IsTrue(templatesByFight.TryGetValue(fight, out var template),
                        $"'{factionId}' is missing a dedicated enemy template for fight {fight}");

                    var board = template.BuildBoard(faction, _registry);
                    Assert.AreEqual(template.placements.Length, board.Pieces.Count,
                        $"'{factionId}' fight {fight}: every authored placement must land");

                    int effective = ArmyStrengthCalculator.Evaluate(board).EffectiveTotal;
                    int target = EnemyLadder.TargetStrength(fight);
                    Assert.LessOrEqual(System.Math.Abs(effective - target) / (float)target, LadderBandTolerance,
                        $"'{factionId}' fight {fight} (effective {effective}) must sit within ±5% of the ladder target {target}");

                    Assert.Greater(effective, previous,
                        $"'{factionId}' fight {fight} (effective {effective}) must outgun fight {fight - 1} (effective {previous})");
                    previous = effective;
                }
            }
        }

        [Test]
        public void EnemyTemplates_FightThreeOnward_FireAtLeastOneSynergy()
        {
            RequireDatabase();

            for (int fight = 3; fight <= 10; fight++)
            {
                var board = BuildTemplateBoard(_database.GetEnemyTemplate(fight));
                var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
                Assert.Greater(snapshot.Links.Count, 0,
                    $"fight {fight} template must anchor at least one live fight-start aura (medic/marshal/iron horse/bulwark adjacency)");
            }
        }

        [Test]
        public void BossStages_EveryBoardBuilds_AndEffectiveTotalEscalates()
        {
            RequireDatabase();

            foreach (var boss in BossRoster.All)
            {
                int previous = 0;
                for (int stage = 0; stage < boss.StageLoadouts.Count; stage++)
                {
                    // BuildStageBoard throws if any placement fails on the 6x6 board.
                    var board = BossRoster.BuildStageBoard(boss, stage, _registry);
                    Assert.AreEqual(boss.StageLoadouts[stage].Count, board.Pieces.Count,
                        $"{boss.BossId} stage {stage}: every authored placement must land");

                    int effective = ArmyStrengthCalculator.Evaluate(board).EffectiveTotal;
                    Assert.Greater(effective, previous,
                        $"{boss.BossId} stage {stage} (effective {effective}) must outgun stage {stage - 1} (effective {previous})");
                    previous = effective;
                }
            }
        }

        /// <summary>PROVISIONAL 2026-07-19 owner spec (deep balance pass): bosses are a clear
        /// step up — each stage board is normalized to BossRoster.BossStrengthRatio (1.5x) of
        /// the concurrent normal-fight ladder strength. Expected computed from primitives,
        /// independent of BossRoster.StageTargetStrength: stage s fires at Dread threshold
        /// 6/12/18 → FightEquivalent 4/7/10 → 1.5 x (354/628/1112) = 531/942/1668. ±6% band
        /// (one point looser than the template band: boss boards stack rares whose per-piece
        /// integer rounding steps are coarser).</summary>
        [Test]
        public void BossStages_EffectiveTotal_TracksOnePointFiveTimesConcurrentLadder()
        {
            RequireDatabase();

            foreach (var boss in BossRoster.All)
            {
                for (int stage = 0; stage < boss.StageLoadouts.Count; stage++)
                {
                    int threshold = DreadRules.NextThreshold(stage);
                    int fight = System.Math.Clamp(DreadRules.FightEquivalent(threshold), 1, 10);
                    int expected = (int)System.Math.Round(
                        BossRoster.BossStrengthRatio * EnemyLadder.TargetStrength(fight));
                    Assert.AreEqual(expected, BossRoster.StageTargetStrength(stage),
                        $"stage {stage}: StageTargetStrength must be 1.5x the concurrent ladder value");

                    var board = BossRoster.BuildStageBoard(boss, stage, _registry);
                    int effective = ArmyStrengthCalculator.Evaluate(board).EffectiveTotal;
                    Assert.LessOrEqual(System.Math.Abs(effective - expected) / (float)expected, 0.06f,
                        $"{boss.BossId} stage {stage} (effective {effective}) must sit within ±6% of {expected}");
                }
            }
        }

        [Test]
        public void DeathShockDamage_SoftenedToSix()
        {
            Assert.AreEqual(6, MoraleRules.DeathShockDamage,
                "2026-07-12 balance pass: packed blobs should bleed morale, not domino (34-unit cascade smoke)");
        }

        private void RequireDatabase()
        {
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
        }

        /// <summary>BuildBoard throws on any overlapping or out-of-bounds anchor, so a
        /// returned board with every placement landed IS the placement assertion.</summary>
        private BoardState BuildTemplateBoard(EnemyTemplateSO template)
        {
            var board = template.BuildBoard(_faction, _registry);
            Assert.AreEqual(template.placements.Length, board.Pieces.Count,
                $"'{template.displayName}': every authored placement must land on the enemy board");
            return board;
        }
    }
}
