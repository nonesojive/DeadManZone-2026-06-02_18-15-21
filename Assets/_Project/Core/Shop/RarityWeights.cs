using System;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Shop
{
    /// <summary>
    /// Per-tier shop offer weights keyed by the Dread difficulty clock
    /// (DreadRules.FightEquivalent), plus the rare-pity rules layered on top (M3).
    /// All values are M3 initial — tune in playtest.
    /// </summary>
    public static class RarityWeights
    {
        public readonly struct Row
        {
            public Row(int minFightEquivalent, int common, int uncommon, int rare)
            {
                MinFightEquivalent = minFightEquivalent;
                CommonPercent = common;
                UncommonPercent = uncommon;
                RarePercent = rare;
            }

            public int MinFightEquivalent { get; }
            public int CommonPercent { get; }
            public int UncommonPercent { get; }
            public int RarePercent { get; }
        }

        // A TABLE, deliberately: a future 4th tier is a new column and the curve is
        // retuned by editing rows — never a hardcoded trio in the generator.
        // Rows are ascending by MinFightEquivalent; each sums to 100 and the rare
        // share never decreases as the clock climbs (test-locked).
        private static readonly Row[] Table =
        {
            new Row(1, 80, 18, 2),
            new Row(3, 74, 22, 4),
            new Row(5, 68, 25, 7),
            new Row(7, 62, 28, 10),
            new Row(9, 55, 30, 15)
        };

        public static Row WeightsFor(int fightEquivalent)
        {
            var row = Table[0];
            for (int i = 1; i < Table.Length; i++)
            {
                if (Table[i].MinFightEquivalent > fightEquivalent)
                    break;
                row = Table[i];
            }

            return row;
        }

        // ---- Rare pity (M3, appear-reset design) ----
        // One counted event = one generated offer BATCH (the round roll and every
        // reroll). Each rare-less batch adds PityStepPercent to the rare share of
        // the NEXT batch; the counter resets when a batch CONTAINS a rare-or-above
        // (appearing, not purchased — the orchestrator owns the RunState mutation).

        /// <summary>Added to the rare share per rare-less batch.</summary>
        public const int PityStepPercent = 4;

        /// <summary>Rare-less batches after which the next batch force-includes one
        /// rare-capable slot (lane fallback still applies: if no lane in the batch
        /// can host a rare, nothing is forced and the counter keeps climbing).</summary>
        public const int PityGuaranteeBatches = 9;

        /// <summary>Pity-boosted rare odds clamp here; reaching the cap also forces
        /// the next batch, so late-game (high base rare) forcing can arrive before
        /// the batch-count guarantee.</summary>
        public const int RareOddsCapPercent = 40;

        /// <summary>Rare-or-above share for the next batch: table base + pity, clamped.</summary>
        public static int RareChancePercent(int fightEquivalent, int rarePityBatches) =>
            Math.Min(
                WeightsFor(fightEquivalent).RarePercent
                    + PityStepPercent * Math.Max(0, rarePityBatches),
                RareOddsCapPercent);

        /// <summary>True when the batch generated at this pity level must force-include
        /// one rare-capable slot (guarantee count reached, or odds at the cap).</summary>
        public static bool ForcesRare(int fightEquivalent, int rarePityBatches) =>
            rarePityBatches >= PityGuaranteeBatches
            || RareChancePercent(fightEquivalent, rarePityBatches) >= RareOddsCapPercent;

        /// <summary>Rolls an offer's rarity tier. Pity feeds the rare share from the
        /// common share first, then uncommon, so the total stays 100.</summary>
        public static Rarity RollTier(Rng rng, int fightEquivalent, int rarePityBatches)
        {
            var row = WeightsFor(fightEquivalent);
            int rare = RareChancePercent(fightEquivalent, rarePityBatches);
            int uncommon = row.UncommonPercent;
            int common = 100 - rare - uncommon;
            if (common < 0)
            {
                uncommon += common;
                common = 0;
            }

            int roll = rng.NextInt(0, 100);
            if (roll < rare)
                return Rarity.Rare;
            if (roll < rare + uncommon)
                return Rarity.Uncommon;
            return Rarity.Common;
        }
    }
}
