namespace DeadManZone.Core.Run
{
    /// <summary>
    /// The Dread run clock (ADR-0004): Dread is earned only by winning normal fights,
    /// fixed thresholds force a Boss Fight, and the third boss win ends the run.
    /// Pure rules — the orchestrator owns applying them to RunState.
    /// </summary>
    public static class DreadRules
    {
        /// <summary>The Normal-tier win rate, and the divisor of the difficulty clock
        /// (<see cref="FightEquivalent"/>). Tier-specific grants live in <see cref="DreadFor"/>.</summary>
        public const int DreadPerWin = 2;

        // ---- M2 Fight Option economy (initial values, tune in playtest) ----

        /// <summary>Authority debited from the round's command pool for taking the easy front.</summary>
        public const int EasyAuthorityCost = 2;

        /// <summary>Hard-front victory materiel package: supplies plus manpower worth
        /// roughly half a muster (IronMarch musters ~9-12 with a couple of supply
        /// buildings: base 1 + 3/depot + 2/synergy pair; other factions' base is 12).</summary>
        public const int HardVictorySupplies = 15;
        public const int HardVictoryManpower = 6;

        public const int BossCount = 3;

        /// <summary>Dread earned by WINNING at the given Fight Option tier (ADR-0004:
        /// harder assaults escalate the war faster). Losses and boss fights grant zero.</summary>
        public static int DreadFor(FightOptionTier tier) => tier switch
        {
            FightOptionTier.Easy => 1,
            FightOptionTier.Hard => 3,
            _ => DreadPerWin
        };

        private static readonly int[] Thresholds = { 6, 12, 18 };

        /// <summary>Dread level that triggers the next Boss Fight. After the third boss
        /// the run is already won; callers should not ask (returns int.MaxValue).</summary>
        public static int NextThreshold(int bossesDefeated) =>
            bossesDefeated >= BossCount ? int.MaxValue : Thresholds[System.Math.Max(0, bossesDefeated)];

        /// <summary>True when the NEXT combat must be a Boss Fight.</summary>
        public static bool IsBossPending(int dread, int bossesDefeated) =>
            bossesDefeated < BossCount && dread >= NextThreshold(bossesDefeated);

        /// <summary>
        /// Difficulty-clock mapping that preserves today's fight-indexed curves: at
        /// DreadPerWin per normal win, N wins land exactly on old fight N+1
        /// (0 Dread = fight 1). Everything difficulty-curved (enemy templates, shop
        /// tiering, morale scale) keys off this instead of FightIndex. Retuned in M2.
        /// </summary>
        public static int FightEquivalent(int dread) => dread / DreadPerWin + 1;
    }
}
