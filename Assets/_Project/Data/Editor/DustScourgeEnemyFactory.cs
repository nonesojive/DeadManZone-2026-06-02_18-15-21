using System.Linq;
using DeadManZone.Core;
using static DeadManZone.Data.Editor.EnemyTemplateAnchors;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): Dust Scourge's 10-fight enemy ladder — commons only through fight 6
    /// (raiders/outriders/gasflingers/rust spears, the wasteland's rank and file), Raid Captain
    /// (Uncommon) debuting fight 5, first Rare (Stormcaller of the Yellow Wind) at fight 7, and
    /// Warlord of Many Banners joining at fight 9. Every fight is a strict superset of the
    /// previous one's placements (never remove/move a piece, only add or upgrade a slot via
    /// EnemyTemplateAnchors.Swap) — this alone guarantees ArmyStrengthCalculator.EffectiveTotal
    /// is non-decreasing fight over fight (BalancePassTests), with no need to hand-verify
    /// synergy adjacency. Placements use EnemyTemplateAnchors' 9-slot grid (Single/
    /// HorizontalPair/VerticalPair/Square2x2 only — this roster's one Triple3 piece,
    /// Corpse-Tithe Caravan, is skipped for the same collision-avoidance reason IronMarch's own
    /// ladder never uses a 3-cell straight piece either).
    /// </summary>
    internal static class DustScourgeEnemyFactory
    {
        private const string Faction = FactionIds.DustScourge;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.Where(p => p.factionId == Faction).ToDictionary(p => p.id);
            var A = byId["waste_raider"];
            var B = byId["outrider"];
            var C = byId["gasflinger"];
            var D = byId["rust_spear"];
            var E = byId["vulture_crew"];
            var U = byId["raid_captain"];
            var R1 = byId["stormcaller_of_the_yellow_wind"];
            var R2 = byId["warlord_of_many_banners"];

            var f1 = new[] { Place(A, P6), Place(A, P8) };
            var f2 = f1.Append(Place(A, P5)).ToArray();
            var f3 = f2.Append(Place(B, P4)).ToArray();
            var f4 = f3.Append(Place(E, P9)).ToArray();
            var f5 = f4.Append(Place(U, P7)).ToArray();
            var f6 = f5.Append(Place(C, P3)).ToArray();
            var f7 = f6.Append(Place(R1, P2)).ToArray();
            var f8 = f7.Append(Place(D, P1)).ToArray();
            var f9 = Swap(f8, P1, R2); // upgrade: D (common) -> Warlord of Many Banners (rare)
            var f10 = Swap(f9, P5, R1); // upgrade: A (common) -> a second Stormcaller

            return new[]
            {
                Save(1, "Scrap Patrol", "Raiders", f1),
                Save(2, "Wasteland Line", "Raiders", f2),
                Save(3, "Scavenger Column", "Raiders", f3),
                Save(4, "Salvage Screen", "Support", f4),
                Save(5, "Raid Captain's Muster", "Command", f5),
                Save(6, "Gasflinger Wedge", "Gas", f6),
                Save(7, "Yellow Wind Rising", "Gas", f7),
                Save(8, "Full Scavenge", "Raiders", f8),
                Save(9, "Banner of Many Tribes", "Boss", f9),
                Save(10, "The Wasteland's Fury", "Boss", f10)
            };
        }

        private static EnemyTemplateSO Save(int fight, string name, string tag, EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements, folder: Faction);
    }
}
