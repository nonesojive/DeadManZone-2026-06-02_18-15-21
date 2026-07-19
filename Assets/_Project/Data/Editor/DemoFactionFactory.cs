using DeadManZone.Core;
using DeadManZone.Data;

namespace DeadManZone.Data.Editor
{
    internal static class DemoFactionFactory
    {
        // 2026-07-15 faction-roster-v1 Wave 2: crimson_legion/ash_wraiths entries replaced by
        // crimson_assembly/ashen_covenant (their real content pass lives in
        // CrimsonAssemblyContentFactory/AshenCovenantContentFactory — this legacy "5 Factions"
        // pipeline only needs a self-consistent FactionSO per id so it still compiles/runs).
        internal static FactionSO[] CreateAll() =>
            new[]
            {
                // PROVISIONAL — economy pass 2026-07-19: mirrors IronmarchUnionContentFactory's
                // 25/12 so this legacy pipeline can't clobber the tuned values on regen.
                DemoContentGenerator.SaveFaction(FactionIds.IronmarchUnion, "IronMarch Union",
                    startingSupplies: 25,
                    startingManpower: 15,
                    baseSuppliesPerRound: 12,
                    baseMusterPerShop: 1,
                    startingAuthority: 2,
                    baseSalvageChancePercent: 1),
                DemoContentGenerator.SaveFaction(FactionIds.DustScourge, "Dust Scourge",
                    startingManpower: 112),
                DemoContentGenerator.SaveFaction(FactionIds.CartelOfEchoes, "Cartel of Echoes",
                    startingAuthority: 5),
                DemoContentGenerator.SaveFaction("neutral", "Neutral Militia", startingSupplies: 80),
                DemoContentGenerator.SaveFaction(FactionIds.CrimsonAssembly, "Crimson Assembly"),
                DemoContentGenerator.SaveFaction(FactionIds.AshenCovenant, "Ashen Covenant")
            };
    }
}
