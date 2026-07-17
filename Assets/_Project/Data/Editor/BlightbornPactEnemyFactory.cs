using System.Linq;
using DeadManZone.Core;
using static DeadManZone.Data.Editor.EnemyTemplateAnchors;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): Blightborn Pact's 10-fight enemy ladder — threadbare guards behind a
    /// gas-count censer carrier through fight 6 (gas walls arrive early, per the design brief),
    /// Gas Alchemist and Widow of the House (Uncommon) debuting fights 5/7, Duchess of Sighs
    /// (Rare, the gas-to-morale fusion) at fight 8, Vitriol Throne (Rare gas structure) at fight
    /// 9, and a second Widow of the House at fight 10. Superset-growth per fight (see
    /// DustScourgeEnemyFactory's class doc). The Yellow Autumn (a pure HQ economy building) is
    /// skipped, same as every other faction's economy buildings.
    /// </summary>
    internal static class BlightbornPactEnemyFactory
    {
        private const string Faction = FactionIds.BlightbornPact;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.Where(p => p.factionId == Faction).ToDictionary(p => p.id);
            var A = byId["threadbare_guard"];
            var B = byId["censer_carrier"];
            var C = byId["iron_veil_guard"];
            var D = byId["court_physician"];
            var U1 = byId["gas_alchemist"];
            var U2 = byId["widow_of_the_house"];
            var R1 = byId["duchess_of_sighs"];
            var R2 = byId["vitriol_throne"];

            var f1 = new[] { Place(A, P6), Place(A, P8) };
            var f2 = f1.Append(Place(A, P5)).ToArray();
            var f3 = f2.Append(Place(B, P4)).ToArray();
            var f4 = f3.Append(Place(D, P9)).ToArray();
            var f5 = f4.Append(Place(U1, P7)).ToArray();
            var f6 = f5.Append(Place(C, P3)).ToArray();
            var f7 = f6.Append(Place(U2, P2)).ToArray();
            var f8 = f7.Append(Place(R1, P1)).ToArray();
            var f9 = Swap(f8, P9, R2); // upgrade: Court Physician -> Vitriol Throne
            var f10 = Swap(f9, P5, U2); // upgrade: threadbare guard -> a second Widow of the House

            return new[]
            {
                Save(1, "Threadbare Line", "Infantry", f1),
                Save(2, "Moth-Eaten Column", "Infantry", f2),
                Save(3, "Censer Screen", "Gas", f3),
                Save(4, "Physician's Detail", "Support", f4),
                Save(5, "Alchemist's Cloud", "Gas", f5),
                Save(6, "Iron Veil Wedge", "Infantry", f6),
                Save(7, "Widow's Column", "Command", f7),
                Save(8, "The Duchess's Sighs", "Boss", f8),
                Save(9, "The Vitriol Throne", "Boss", f9),
                Save(10, "The Rot in Full Bloom", "Boss", f10)
            };
        }

        private static EnemyTemplateSO Save(int fight, string name, string tag, EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements, folder: Faction);
    }
}
