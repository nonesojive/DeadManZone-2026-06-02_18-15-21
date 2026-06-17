using DeadManZone.Core.Common;

namespace DeadManZone.Core.Shop
{
    public static class ShopOfferSourceRoller
    {
        public static ShopOfferSource Roll(ShopOfferWeights weights, Rng rng)
        {
            int roll = rng.NextInt(0, 100);
            if (roll < weights.NeutralPercent)
                return ShopOfferSource.Neutral;

            roll -= weights.NeutralPercent;
            if (roll < weights.SalvagePercent)
                return ShopOfferSource.Salvage;

            return ShopOfferSource.Faction;
        }
    }
}
