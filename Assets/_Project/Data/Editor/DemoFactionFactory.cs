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
                    startingSupplies: 50,
                    startingManpower: 15,
                    baseSuppliesPerRound: 10,
                    baseMusterPerShop: 1,
                    startingAuthority: 2,
                    baseSalvageChancePercent: 1),
                DemoContentGenerator.SaveFaction(FactionIds.DustScourge, "Dust Scourge",
                    startingManpower: 112),
                DemoContentGenerator.SaveFaction(FactionIds.CartelOfEchoes, "Cartel of Echoes",
                    startingAuthority: 5),
                DemoContentGenerator.SaveFaction("neutral", "Neutral Militia", startingSupplies: 80),
                DemoContentGenerator.SaveFaction("crimson_legion", "Crimson Legion"),
                DemoContentGenerator.SaveFaction("ash_wraiths", "Ash Wraiths")
            };
    }
}
