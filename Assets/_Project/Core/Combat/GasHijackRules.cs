namespace DeadManZone.Core.Combat
{
    /// <summary>2026-07-15 faction-roster-v1 §2.7/§4 (🟡 ambient-gas hijack, Blightborn's Yellow
    /// Autumn): "the ambient anti-stall gas starts far earlier and YOUR units are immune to
    /// it." Sanctioned reuse of GasDamageSystem — this class only resolves the earlier start
    /// tick; the per-side immunity check lives inline in TickCombatRun.ApplyGas (it needs the
    /// combatant's Side, which this pure-rules seam doesn't see).</summary>
    public static class GasHijackRules
    {
        // PROVISIONAL — tune in playtest; watch pacing per the roster spec's own callout.
        public const int EarlyGasStartTick = 120;

        public static int GetEffectiveGasStartTick(bool hijacked) =>
            hijacked ? EarlyGasStartTick : CombatPacingConfig.GasStartTick;
    }
}
