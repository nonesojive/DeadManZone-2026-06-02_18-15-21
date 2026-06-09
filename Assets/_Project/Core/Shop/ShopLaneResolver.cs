using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Shop
{
    public enum ShopLaneResolveConfidence
    {
        Mapped,
        SpecialtyPendingRules,
        UnknownRoleFallback
    }

    public readonly struct ShopLaneResolveResult
    {
        public ShopLaneResolveResult(ShopLane lane, ShopLaneResolveConfidence confidence)
        {
            Lane = lane;
            Confidence = confidence;
        }

        public ShopLane Lane { get; }
        public ShopLaneResolveConfidence Confidence { get; }
    }

    /// <summary>Derives shop lane from combat role tags.</summary>
    public static class ShopLaneResolver
    {
        public static ShopLaneResolveResult Resolve(string combatRole)
        {
            if (string.IsNullOrWhiteSpace(combatRole))
                return new ShopLaneResolveResult(ShopLane.Offensive, ShopLaneResolveConfidence.UnknownRoleFallback);

            switch (combatRole.Trim())
            {
                case GameTagIds.Support:
                case GameTagIds.Utility:
                case GameTagIds.Headquarters:
                case GameTagIds.Defender:
                    return new ShopLaneResolveResult(ShopLane.Defensive, ShopLaneResolveConfidence.Mapped);

                case GameTagIds.Assault:
                case GameTagIds.Sniper:
                case GameTagIds.Tank:
                    return new ShopLaneResolveResult(ShopLane.Offensive, ShopLaneResolveConfidence.Mapped);

                case GameTagIds.Artillery:
                    if (SpecialtyLaneRuleCatalog.TryResolveSpecialty(combatRole, out var specialtyLane))
                        return new ShopLaneResolveResult(specialtyLane, ShopLaneResolveConfidence.Mapped);
                    return new ShopLaneResolveResult(ShopLane.Specialty, ShopLaneResolveConfidence.SpecialtyPendingRules);

                default:
                    return new ShopLaneResolveResult(ShopLane.Offensive, ShopLaneResolveConfidence.UnknownRoleFallback);
            }
        }

        public static ShopLane ResolveLane(string combatRole) => Resolve(combatRole).Lane;
    }
}
