namespace DeadManZone.Core.Shop
{
    public static class ShopOfferWeightResolver
    {
        public static ShopOfferWeights Resolve(
            ShopSlotProfile profile,
            int computedSalvagePercent,
            bool hasEnemyFaction,
            ShopOfferWeightPatch patch = default)
        {
            var weights = profile.BaseWeights.WithSalvageLeg(computedSalvagePercent, hasEnemyFaction);
            if (patch.NeutralDelta == 0 && patch.FactionDelta == 0 && patch.SalvageDelta == 0)
                return weights;

            return weights.ApplyPatch(patch);
        }
    }
}
