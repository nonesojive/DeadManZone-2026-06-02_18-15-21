using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;

namespace DeadManZone.Core.Shop
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1 §1.5: the salvage pity timer. Same architecture as
    /// RarityWeights' rare pity — state-derived, counts SHOWN batches (appear-reset), no
    /// extra randomness. A salvage-drought counter forces a salvage-source offer after the
    /// faction's dry-batch threshold (global default 4; Dust Scourge tightens to 2 via
    /// FactionPassives). Edge case: if the salvage pool is empty the counter HOLDS — the
    /// orchestrator must skip calling <see cref="ContainsSalvageOffer"/>-driven updates
    /// while <c>SalvagePoolAvailability.IsEmpty</c> is true, so it neither resets nor climbs.
    /// </summary>
    public static class SalvagePityRules
    {
        /// <summary>True when the batch generated at this pity level must force-include
        /// one salvage-source offer (lane fallback still applies — if no lane in the batch
        /// can host a salvage-source offer, nothing is forced and the counter keeps climbing).</summary>
        public static bool ForcesSalvage(string factionId, int salvagePityBatches) =>
            salvagePityBatches >= FactionPassives.SalvagePityDryBatchThreshold(factionId);

        /// <summary>Whether a generated batch shows a salvage-sourced offer — the pity
        /// counter's reset condition (appearing, not purchased).</summary>
        public static bool ContainsSalvageOffer(IEnumerable<ShopOffer> offers) =>
            offers != null && offers.Any(o => o.IsSalvaged);
    }
}
