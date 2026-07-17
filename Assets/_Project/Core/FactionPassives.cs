namespace DeadManZone.Core
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1 §1.9/§4: the single home for the per-faction
    /// economy/shop passives (mercenary slot, salvage pity tightening, rarity-odds
    /// shift, Despair Dividend, Paradox's free reroll). NOT a plugin framework — each
    /// passive is a small static query gated on a FactionIds constant, mirroring
    /// MoraleRules.IsDeathShockInverted (W1a). Every query is a no-op (false/0/unchanged)
    /// for any faction that doesn't own the passive, including factions with no
    /// FactionSO content yet. All magnitudes are PROVISIONAL — tune in playtest.
    /// </summary>
    public static class FactionPassives
    {
        // ---- Cartel of Echoes: mercenary shop slot ----

        /// <summary>Surcharge on the mercenary slot's price, percent. Freelance Colonel
        /// (a later content wave's rare) reduces this 25→10 — kept as a queryable
        /// method rather than a constant so that wave can hook in without touching the
        /// slot-generation logic (CartelMercenarySlotProvider / ShopGenerator).</summary>
        public const int MercenarySurchargePercent = 25; // PROVISIONAL

        public static bool HasMercenarySlot(string factionId) =>
            factionId == FactionIds.CartelOfEchoes;

        public static int MercenarySurchargeFor(string factionId) =>
            HasMercenarySlot(factionId) ? MercenarySurchargePercent : 0;

        // ---- Salvage pity timer (§1.5) — global rule; Dust Scourge tightens it ----

        /// <summary>Dry-batch threshold before a salvage-source offer is forced, globally.</summary>
        public const int SalvagePityDryBatchThresholdDefault = 4; // PROVISIONAL

        /// <summary>Dust Scourge's passive tightens the global threshold.</summary>
        public const int SalvagePityDryBatchThresholdDustScourge = 2; // PROVISIONAL

        public static int SalvagePityDryBatchThreshold(string factionId) =>
            factionId == FactionIds.DustScourge
                ? SalvagePityDryBatchThresholdDustScourge
                : SalvagePityDryBatchThresholdDefault;

        // ---- Crimson Assembly: "Ahead of Schedule" rarity-odds shift ----

        /// <summary>Shop RARITY ODDS roll as if FightEquivalent were this much higher.
        /// Prices are untouched — callers must keep using the real FightEquivalent for
        /// price math (ShopGenerator.CreateOffer's Dread tax).</summary>
        public const int CrimsonRarityOddsFightEquivalentBonus = 1; // PROVISIONAL

        public static int RarityOddsFightEquivalent(string factionId, int fightEquivalent) =>
            factionId == FactionIds.CrimsonAssembly
                ? fightEquivalent + CrimsonRarityOddsFightEquivalentBonus
                : fightEquivalent;

        // ---- Blightborn Pact: "Despair Dividend" ----

        /// <summary>Flat Supplies granted per enemy unit that routed this fight (routing,
        /// not killing — the sim already denies salvage for routed enemies; this is the
        /// compensating economy hook, applied regardless of win/loss).</summary>
        public const int DespairDividendSupplyPerRout = 1; // PROVISIONAL

        public static int DespairDividendSupplies(string factionId, int enemyRoutedCount)
        {
            if (factionId != FactionIds.BlightbornPact || enemyRoutedCount <= 0)
                return 0;

            return enemyRoutedCount * DespairDividendSupplyPerRout;
        }

        // ---- Paradox Engine: free first reroll each Build ----

        /// <summary>The first shop reroll each Build phase is free (Supplies cost only —
        /// Authority lock costs are untouched).</summary>
        public static bool HasFreeFirstReroll(string factionId) =>
            factionId == FactionIds.ParadoxEngine;
    }
}
