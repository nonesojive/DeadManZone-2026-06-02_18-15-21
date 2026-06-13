namespace DeadManZone.Data.Editor
{
    internal static class DemoFactionFactory
    {
        public static FactionSO[] CreateAll() => new[]
        {
            DemoContentGenerator.SaveFaction("iron_vanguard", "IronMarch Union", "ironmarch_hq",
                baseMusterPerShop: 12),
            DemoContentGenerator.SaveFaction("dust_scourge", "Dust Scourge", "dust_hq",
                baseMusterPerShop: 10),
            DemoContentGenerator.SaveFaction("cartel_of_echoes", "Cartel of Echoes", "echo_hq",
                startingAuthority: 3, baseMusterPerShop: 14),
            DemoContentGenerator.SaveFaction("neutral", "Neutral Militia", "ironmarch_hq", startingSupplies: 80),
            DemoContentGenerator.SaveFaction("crimson_legion", "Crimson Legion", "ironmarch_hq"),
            DemoContentGenerator.SaveFaction("ash_wraiths", "Ash Wraiths", "ironmarch_hq", startingMorale: 90)
        };
    }
}
