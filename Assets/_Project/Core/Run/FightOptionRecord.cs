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

        /// <summary>Army effective strength (ArmyStrengthCalculator EffectiveTotal — the
        /// same number the matchup HUD shows) for the report's strength band.</summary>
        public int StrengthPreview { get; set; }
    }
}
