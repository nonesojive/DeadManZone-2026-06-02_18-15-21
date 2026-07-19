using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Run
{
    /// <summary>One authored enemy army the generator may roll — a Core-side view of an
    /// enemy template; BuildBoard defers the Data-side board construction.</summary>
    public sealed class FightOptionArmySource
    {
        public int FightNumber { get; init; }
        public string EnemyFactionId { get; init; }
        public Func<BoardState> BuildBoard { get; init; }
    }

    /// <summary>
    /// Rolls the round's three Fight Options from the "options" sub-stream keyed on
    /// FightIndex — the plain round counter. A re-fought loss keeps its FightIndex (and
    /// Dread), so it re-rolls the SAME options: the same fronts after a defeat is
    /// intended, and mid-round regeneration is impossible because generation only runs
    /// where the round begins (anti-scum).
    /// Three armies are drawn INDEPENDENTLY (fightNumber within ±1 of the Dread difficulty
    /// clock, clamped to the authored range; armies/pools may repeat) and a Battle
    /// Condition is drawn unconditionally right after — that is the entire rng draw
    /// sequence, fixed for a given seed. ONLY THEN are tiers assigned: the three drawn
    /// armies are sorted by strength and handed out weakest→Easy, middle→Normal,
    /// strongest→Hard (2026-07-17 fix — tiers used to be assigned by draw order/slot
    /// index, so an uneven per-faction pool could roll a "hard" option weaker than its
    /// own "easy" one). The Battle Condition always lands on whichever army is now Hard,
    /// which may not be the army that was drawn last.
    /// After tiering, Easy and Hard are STAT-SCALED to deliberate ratios of Normal's
    /// EffectiveTotal (0.85x / 1.30x — PROVISIONAL 2026-07-19 owner spec) via
    /// <see cref="BoardStrengthScaler"/>; the solved scale rides on the option record so the
    /// fight-time template rebuild fields the same army the report displayed.
    /// </summary>
    public static class FightOptionGenerator
    {
        public const int OptionCount = 3;
        public const int FightNumberSpread = 1;

        // PROVISIONAL 2026-07-19 owner spec: the three fronts' DISPLAYED strengths sit in
        // deliberate ratios to the Normal draw's EffectiveTotal (±5%), enforced by solving a
        // uniform per-piece StatScale (BoardStrengthScaler) — the ±1-band draws alone gave
        // accidental spreads. Normal's board is never ratio-scaled; its record carries the
        // build-time ladder-normalization scale (EnemyLadder — see the tier loop).
        public const float EasyStrengthRatio = 0.85f;
        public const float HardStrengthRatio = 1.30f;

        public static List<FightOptionRecord> Generate(
            int runSeed,
            int roundIndex,
            int dread,
            IReadOnlyList<FightOptionArmySource> armies)
        {
            var ordered = (armies ?? Array.Empty<FightOptionArmySource>())
                .Where(a => a != null)
                .OrderBy(a => a.FightNumber)
                .ThenBy(a => a.EnemyFactionId, StringComparer.Ordinal)
                .ToList();
            if (ordered.Count == 0)
                throw new ArgumentException("At least one enemy army source is required.", nameof(armies));

            int minFight = ordered[0].FightNumber;
            int maxFight = ordered[ordered.Count - 1].FightNumber;
            int target = Math.Clamp(DreadRules.FightEquivalent(dread), minFight, maxFight);

            var candidates = ordered
                .Where(a => Math.Abs(a.FightNumber - target) <= FightNumberSpread)
                .ToList();
            if (candidates.Count == 0)
            {
                // Defensive: a gap in the authored fight numbers around the target.
                int nearest = ordered.Min(a => Math.Abs(a.FightNumber - target));
                candidates = ordered
                    .Where(a => Math.Abs(a.FightNumber - target) == nearest)
                    .ToList();
            }

            var rng = SeedStreams.Stream(runSeed, "options", roundIndex);

            // ---- Draft phase: EXACTLY the original rng draw sequence -------------------
            // 3 army picks (slot 0, 1, 2, in that order), each with its own arena-theme roll
            // (own stream cell, keyed on slot — unaffected by anything below) and a "raw"
            // strength reading taken on ONE consistent basis (engines always included) so the
            // three candidates can be compared apples-to-apples before tier — and therefore
            // Easy's engine suppression — is decided. Nothing here depends on tier, so this
            // loop's rng.NextInt calls land on the same stream cells as before the fix.
            var drafted = new (FightOptionArmySource Army, string ThemeId, BoardState Board, int RawStrength)[OptionCount];
            for (int slot = 0; slot < OptionCount; slot++)
            {
                var army = candidates[rng.NextInt(0, candidates.Count)];
                string themeId = ArenaThemes.Roll(runSeed, roundIndex, slot, army.EnemyFactionId);
                var board = army.BuildBoard();
                int rawStrength = ArmyStrengthCalculator.Evaluate(
                    board, buildBoards: null, includeFightStartEngines: true).EffectiveTotal;
                drafted[slot] = (army, themeId, board, rawStrength);
            }

            // Hard's Battle Condition draw ALWAYS happens here, as the 4th stream cell —
            // previously it fired inline when the loop's slot happened to be Hard (slot 2,
            // since tier used to just BE the slot index). Tier is no longer slot-indexed (see
            // below), so we draw it unconditionally in the same position instead: the NUMBER
            // and ORDER of rng draws for a given seed is unchanged. Which army ends up wearing
            // Hard (and therefore this condition) can now shift — that's the point of the fix.
            string conditionId = ConditionCatalog.Ids[rng.NextInt(0, ConditionCatalog.Ids.Count)];

            // ---- Tier assignment: by strength, not slot -------------------------------
            // Sort basis = RawStrength (engines always included, computed identically for all
            // three above). Assign Easy/Normal/Hard weakest-to-strongest; the ratio-scaling
            // step below then pins the DISPLAYED spread around the middle draw (Easy 0.85x,
            // Hard 1.30x of Normal), which keeps the previews non-decreasing Easy -> Hard by
            // construction (0.85 < 1 < 1.30, modulo the solver's small residual and its
            // [0.4, 2.5] clamps — a clamped Easy still lands below Normal, a clamped Hard
            // still lands above it, because Easy starts weakest and Hard strongest).
            // OrderBy is stable, so ties (e.g. two slots drawing the same army) keep their
            // original slot order rather than reshuffling arbitrarily.
            int[] bySlotAscendingStrength = Enumerable.Range(0, OptionCount)
                .OrderBy(slot => drafted[slot].RawStrength)
                .ToArray();

            // ---- Ratio scaling (PROVISIONAL 2026-07-19 owner spec) -----------------------
            // Pin the displayed spread to Normal's EffectiveTotal: Easy is solved to 0.85x,
            // Hard to 1.30x, via a uniform per-piece StatScale. Normal's board stays untouched
            // — but its RECORD carries the scale the board arrived with, because BuildBoard
            // now ladder-normalizes every template (EnemyLadder, 2026-07-19 deep balance pass)
            // and ApplyScale at fight time SETS StatScale absolutely: recording 1 would strip
            // that normalization on the rebuild. Easy
            // is solved on the ENGINES-SUPPRESSED basis because that is both what it displays
            // (the existing suppressed-preview behavior, kept) and what actually marches on an
            // easy front (TickCombatRun's suppressEnemyFightStartEngines). No rng here — the
            // draw sequence above is unchanged for a given seed. The solved scale is recorded
            // on the option so the fight-time template rebuild (RunOrchestrator.BeginCombat /
            // GetOptionEnemyBoard) fields the SAME army the preview rated.
            int normalRating = drafted[bySlotAscendingStrength[(int)FightOptionTier.Normal]].RawStrength;

            var options = new List<FightOptionRecord>(OptionCount);
            for (int tierIndex = 0; tierIndex < OptionCount; tierIndex++)
            {
                var tier = (FightOptionTier)tierIndex;
                var (army, themeId, board, rawStrength) = drafted[bySlotAscendingStrength[tierIndex]];

                // Default (Normal): the board's build-time scale — BuildBoard's ladder
                // normalization is uniform, so any piece carries it; 1 for an unscaled build.
                float statScale = 1f;
                foreach (var placed in board.Pieces) { statScale = placed.StatScale; break; }
                int preview = rawStrength;
                switch (tier)
                {
                    case FightOptionTier.Easy:
                        statScale = BoardStrengthScaler.SolveScale(
                            board,
                            (int)Math.Round(EasyStrengthRatio * normalRating),
                            includeFightStartEngines: false);
                        // Preview what will actually MARCH: Easy fields a green force with its
                        // fight-start engines suppressed, so it previews on that same basis.
                        preview = ArmyStrengthCalculator.Evaluate(
                            board, buildBoards: null, includeFightStartEngines: false).EffectiveTotal;
                        break;

                    case FightOptionTier.Hard:
                        statScale = BoardStrengthScaler.SolveScale(
                            board,
                            (int)Math.Round(HardStrengthRatio * normalRating),
                            includeFightStartEngines: true);
                        preview = ArmyStrengthCalculator.Evaluate(
                            board, buildBoards: null, includeFightStartEngines: true).EffectiveTotal;
                        break;
                }

                options.Add(new FightOptionRecord
                {
                    Tier = tier,
                    EnemyFactionId = army.EnemyFactionId,
                    TemplateFightNumber = army.FightNumber,
                    ConditionId = tier == FightOptionTier.Hard ? conditionId : null,
                    ThemeId = themeId,
                    StatScale = statScale,
                    StrengthPreview = preview
                });
            }

            return options;
        }
    }
}
