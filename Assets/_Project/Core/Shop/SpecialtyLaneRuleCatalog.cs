namespace DeadManZone.Core.Shop
{
    /// <summary>Placeholder for future specialty lane placement rules.</summary>
    public static class SpecialtyLaneRuleCatalog
    {
        public static bool TryResolveSpecialty(string combatRole, out ShopLane lane)
        {
            lane = default;
            return false;
        }
    }
}
