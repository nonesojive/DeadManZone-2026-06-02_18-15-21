using DeadManZone.Core;

namespace DeadManZone.Data.Editor
{
    internal static class DemoFactionFactory
    {
        public static FactionSO[] CreateAll() => new[]
        {
            DemoContentGenerator.SaveFaction(FactionIds.IronVanguard, "IronMarch Union", "ironmarch_hq",
                startingSupplies: 125, baseMusterPerShop: 12, baseSalvageChancePercent: 10),
            DemoContentGenerator.SaveFaction("FactionIds.DustScourge", "Dust Scourge", "dust_hq",
                baseMusterPerShop: 10, baseSalvageChancePercent: 18),
            DemoContentGenerator.SaveFaction("FactionIds.CartelOfEchoes", "Cartel of Echoes", "echo_hq",
                startingAuthority: 3, baseMusterPerShop: 14, baseSalvageChancePercent: 12),
            DemoContentGenerator.SaveFaction("neutral", "Neutral Militia", "ironmarch_hq", startingSupplies: 80),
            DemoContentGenerator.SaveFaction("crimson_legion", "Crimson Legion", "ironmarch_hq"),
            DemoContentGenerator.SaveFaction("ash_wraiths", "Ash Wraiths", "ironmarch_hq", startingMorale: 90)
        };
    }
}
