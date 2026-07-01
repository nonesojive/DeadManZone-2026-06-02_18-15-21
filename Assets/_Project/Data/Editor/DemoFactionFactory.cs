using DeadManZone.Core;
using DeadManZone.Data;

namespace DeadManZone.Data.Editor
{
    internal static class DemoFactionFactory
    {
        internal static FactionSO[] CreateAll() =>
            new[]
            {
                DemoContentGenerator.SaveFaction(FactionIds.IronmarchUnion, "IronMarch Union",
                    startingSupplies: 125),
                DemoContentGenerator.SaveFaction(FactionIds.DustScourge, "Dust Scourge",
                    startingManpower: 112),
                DemoContentGenerator.SaveFaction(FactionIds.CartelOfEchoes, "Cartel of Echoes",
                    startingAuthority: 5),
                DemoContentGenerator.SaveFaction("neutral", "Neutral Militia", startingSupplies: 80),
                DemoContentGenerator.SaveFaction("crimson_legion", "Crimson Legion"),
                DemoContentGenerator.SaveFaction("ash_wraiths", "Ash Wraiths", startingMorale: 90)
            };
    }
}
