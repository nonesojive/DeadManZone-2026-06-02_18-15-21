namespace DeadManZone.Core.Shop
{
    public enum ShopPoolBias
    {
        Offensive,
        Defensive
    }

    public static class ShopPoolBiasExtensions
    {
        public static ShopLane ToShopLane(this ShopPoolBias bias) =>
            bias == ShopPoolBias.Defensive ? ShopLane.Defensive : ShopLane.Offensive;
    }
}
