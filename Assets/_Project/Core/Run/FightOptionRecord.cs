namespace DeadManZone.Core.Run
{
    /// <summary>The per-round easy/normal/hard front choice (M2, ADR-0004).</summary>
    public enum FightOptionTier
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    /// <summary>
    /// One seeded front on the round's Front Report. Persisted on RunState — schema
    /// stays v9 (additive; older v9 saves deserialize with an empty option list and
    /// take the legacy template path for the round already in progress). The army
    /// itself is never persisted: <see cref="TemplateFightNumber"/> is the regenerable
    /// key into the authored enemy templates.
    /// </summary>
    public sealed class FightOptionRecord
    {
        public FightOptionTier Tier { get; set; }

        /// <summary>Enemy pool of the rolled army (salvage targeting, report intel).</summary>
        public string EnemyFactionId { get; set; }

        /// <summary>Authored template key — the army regenerates from this on demand.</summary>
        public int TemplateFightNumber { get; set; }

        /// <summary>Battle Condition id (hard tier only, else null).</summary>
        public string ConditionId { get; set; }

        /// <summary>Arena Theme rolled from this option's pool (M4). Additive on v9 —
        /// null on older saves; consumers resolve via ArenaThemes.Normalize.</summary>
        public string ThemeId { get; set; }

        /// <summary>Army effective strength (ArmyStrengthCalculator EffectiveTotal — the
        /// same number the matchup HUD shows) for the report's strength band.</summary>
        public int StrengthPreview { get; set; }

        /// <summary>PROVISIONAL 2026-07-19 owner spec (fight-option strength ratios): the
        /// ABSOLUTE uniform per-piece StatScale this front fights at, solved at generation
        /// so its displayed strength sits at its tier ratio of Normal (Easy 0.85x,
        /// Hard 1.30x). Normal records the scale its board arrived with from BuildBoard's
        /// ladder normalization (EnemyLadder, deep balance pass same date) — ApplyScale
        /// SETS StatScale, so recording 1 would strip the normalization at fight time.
        /// The army itself still regenerates from <see cref="TemplateFightNumber"/> — this
        /// is the one extra number needed to rebuild the SAME scaled army at fight time
        /// (RunOrchestrator.BeginCombat / GetOptionEnemyBoard apply it via
        /// BoardStrengthScaler.ApplyScale). Additive on the save schema: absent on older
        /// saves → 1, which fields the raw authored army those older previews rated.</summary>
        public float StatScale { get; set; } = 1f;
    }
}
