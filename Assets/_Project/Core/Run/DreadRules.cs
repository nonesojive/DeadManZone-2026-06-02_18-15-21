namespace DeadManZone.Core.Run
{
    /// <summary>
    /// The Dread run clock (ADR-0004): Dread is earned only by winning normal fights,
    /// fixed thresholds force a Boss Fight, and the third boss win ends the run.
    /// Pure rules — the orchestrator owns applying them to RunState.
    /// </summary>
    public static class DreadRules
    {
        /// <summary>M1 interim flat rate — M2's Fight Options set 1/2/3 Dread per tier.</summary>
        public const int DreadPerWin = 2;

        public const int BossCount = 3;

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
