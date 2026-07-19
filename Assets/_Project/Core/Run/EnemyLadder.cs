namespace DeadManZone.Core.Run
{
    /// <summary>
    /// PROVISIONAL 2026-07-19 owner spec (deep balance pass): the canonical enemy-strength
    /// ladder. Every normal-fight enemy template is NORMALIZED to this curve at build time
    /// (<c>EnemyTemplateSO.BuildBoard</c> solves a uniform per-piece StatScale via
    /// <see cref="BoardStrengthScaler"/>, on the default engines-on Evaluate basis),
    /// replacing the old accidental per-faction authored curves (symptom: IronMarch fight 9
    /// rated 701 vs fight 8's 704; cross-faction template strength varied wildly at the same
    /// fight index). Bosses step up to <see cref="BossRoster.BossStrengthRatio"/> x the
    /// concurrent ladder value (see <see cref="BossRoster.StageTargetStrength"/>).
    ///
    /// TargetStrength(fight) = round(200 x 1.21^(fight-1)). Fights 1..10:
    /// 200, 242, 293, 354, 429, 519, 628, 759, 919, 1112.
    /// (The owner-spec table quoted 760 for fight 8; the exact value is 759.4997, which
    /// rounds to 759 — the formula is canonical, the table entry was a hand-rounding slip.
    /// At +/-5% test bands and 21% ladder gaps the 1-point difference is immaterial.)
    /// </summary>
    public static class EnemyLadder
    {
        public const int BaseStrength = 200;
        public const double GrowthPerFight = 1.21;

        /// <summary>Ladder target EffectiveTotal for a 1-based fight number (values below 1
        /// clamp to fight 1; the formula extends naturally past the authored 10).</summary>
        public static int TargetStrength(int fight)
        {
            if (fight < 1)
                fight = 1;
            return (int)System.Math.Round(
                BaseStrength * System.Math.Pow(GrowthPerFight, fight - 1));
        }
    }
}
