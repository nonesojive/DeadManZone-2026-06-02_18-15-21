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
    /// Rolls the round's three Fight Options (slot 0 easy / 1 normal / 2 hard) from the
    /// "options" sub-stream keyed on FightIndex — the plain round counter. A re-fought
    /// loss keeps its FightIndex (and Dread), so it re-rolls the SAME options: the same
    /// fronts after a defeat is intended, and mid-round regeneration is impossible
    /// because generation only runs where the round begins (anti-scum).
    /// Each slot INDEPENDENTLY picks an army whose fightNumber is within ±1 of the
    /// Dread difficulty clock (clamped to the authored range), so armies/pools may
    /// repeat across slots. Hard additionally draws its Battle Condition from the
    /// same stream cell.
    /// </summary>
    public static class FightOptionGenerator
    {
        public const int OptionCount = 3;
        public const int FightNumberSpread = 1;

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
            var options = new List<FightOptionRecord>(OptionCount);
            for (int slot = 0; slot < OptionCount; slot++)
            {
                var tier = (FightOptionTier)slot;
                var army = candidates[rng.NextInt(0, candidates.Count)];
                options.Add(new FightOptionRecord
                {
                    Tier = tier,
                    EnemyFactionId = army.EnemyFactionId,
                    TemplateFightNumber = army.FightNumber,
                    ConditionId = tier == FightOptionTier.Hard
                        ? ConditionCatalog.Ids[rng.NextInt(0, ConditionCatalog.Ids.Count)]
                        : null,
                    // Own "arenaTheme" stream cell — must not draw from `rng`, or the
                    // theme roll would shift every pre-M4 seed's condition rolls.
                    ThemeId = ArenaThemes.Roll(runSeed, roundIndex, slot, army.EnemyFactionId),

                    // Preview what will actually MARCH, not the raw stat line: rate the enemy with
                    // its fight-start engines applied — EXCEPT on Easy, where the enemy fields a
                    // green force with those engines suppressed (see TickCombatRun's
                    // suppressEnemyFightStartEngines). So Easy previews lower because it genuinely
                    // IS weaker, and all three numbers are finally measured on the same basis.
                    StrengthPreview = ArmyStrengthCalculator.Evaluate(
                        army.BuildBoard(),
                        buildBoards: null,
                        includeFightStartEngines: tier != FightOptionTier.Easy).EffectiveTotal
                });
            }

            return options;
        }
    }
}
