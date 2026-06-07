namespace DeadManZone.Data.Editor
{
    internal static class DemoFactionFactory
    {
        public static FactionSO[] CreateAll() => new[]
        {
            DemoContentGenerator.SaveFaction("iron_vanguard", "Ironmarch Vanguard", "hq_command"),
            DemoContentGenerator.SaveFaction("dust_scourge", "Dust Scourge", "dust_hq", startingManpower: 12),
            DemoContentGenerator.SaveFaction("cartel_of_echoes", "Cartel of Echoes", "echo_hq", startingAuthority: 3),
            DemoContentGenerator.SaveFaction("neutral", "Neutral Militia", "hq_command", startingSupplies: 80),
            DemoContentGenerator.SaveFaction("crimson_legion", "Crimson Legion", "hq_command"),
            DemoContentGenerator.SaveFaction("ash_wraiths", "Ash Wraiths", "hq_command", startingMorale: 90)
        };
    }
}
