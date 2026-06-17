using System;

namespace DeadManZone.Core.Shop
{
    public readonly struct ShopOfferWeights
    {
        public int NeutralPercent { get; }
        public int FactionPercent { get; }
        public int SalvagePercent { get; }

        public ShopOfferWeights(int neutralPercent, int factionPercent, int salvagePercent)
        {
            NeutralPercent = neutralPercent;
            FactionPercent = factionPercent;
            SalvagePercent = salvagePercent;
        }

        public static ShopOfferWeights Default => new(10, 80, 10);

        public ShopOfferWeights WithSalvageLeg(int salvagePercent, bool hasEnemyFaction)
        {
            int neutral = NeutralPercent;
            int salvage = hasEnemyFaction
                ? Math.Clamp(salvagePercent, 0, 100 - neutral)
                : 0;
            int faction = Math.Max(0, 100 - neutral - salvage);
            return new ShopOfferWeights(neutral, faction, salvage);
        }

        public ShopOfferWeights ApplyPatch(ShopOfferWeightPatch patch)
        {
            int neutral = Math.Clamp(NeutralPercent + patch.NeutralDelta, 0, 100);
            int salvage = Math.Clamp(SalvagePercent + patch.SalvageDelta, 0, 100);
            int faction = Math.Max(0, 100 - neutral - salvage);
            return new ShopOfferWeights(neutral, faction, salvage);
        }
    }

    public readonly struct ShopOfferWeightPatch
    {
        public int NeutralDelta { get; init; }
        public int FactionDelta { get; init; }
        public int SalvageDelta { get; init; }

        public static ShopOfferWeightPatch None => default;
    }
}
