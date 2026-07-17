using System.Linq;
using DeadManZone.Core;
using static DeadManZone.Data.Editor.EnemyTemplateAnchors;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): Crimson Assembly's 10-fight enemy ladder — suppression teams from
    /// fight 1 (the tentpole debuff arrives immediately, unlike other factions' commons-only
    /// opener), Fire-Plan Officer and Scout Tankette (Uncommon, the sanctioned Uncommon vehicle)
    /// debuting fights 5/7, then all three Rares stack in by fight 10: Director of Programs
    /// (fight 8), "Vanquisher" Doctrine Tank (fight 9), "Stiller" Suppression Platform (fight
    /// 10) — landing the sanctioned two-rare-tank stack (§1.6) at the top of the ladder.
    /// Superset-growth per fight (see DustScourgeEnemyFactory's class doc).
    /// </summary>
    internal static class CrimsonAssemblyEnemyFactory
    {
        private const string Faction = FactionIds.CrimsonAssembly;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.Where(p => p.factionId == Faction).ToDictionary(p => p.id);
            var A = byId["assembly_trooper"];
            var B = byId["suppression_team"];
            var C = byId["hazmat_vanguard"];
            var D = byId["ballistics_analyst"];
            var E = byId["bunker_emplacement"];
            var U1 = byId["scout_tankette"];
            var U2 = byId["fire_plan_officer"];
            var R1 = byId["vanquisher_doctrine_tank"];
            var R2 = byId["stiller_suppression_platform"];
            var R3 = byId["director_of_programs"];

            var f1 = new[] { Place(B, P6), Place(B, P8) };
            var f2 = f1.Append(Place(A, P5)).ToArray();
            var f3 = f2.Append(Place(D, P4)).ToArray();
            var f4 = f3.Append(Place(C, P9)).ToArray();
            var f5 = f4.Append(Place(U2, P7)).ToArray();
            var f6 = f5.Append(Place(E, P3)).ToArray();
            var f7 = f6.Append(Place(U1, P2)).ToArray();
            var f8 = f7.Append(Place(R3, P1)).ToArray();
            // f9/f10 swap-safety (2026-07-17, caught by BalancePassTests via Cartel's identical
            // bug — see CartelOfEchoesEnemyFactory's comment): "Vanquisher" (10 dmg/140 HP/heavy
            // armor) safely replaces Hazmat Vanguard (5 dmg/65 HP/medium armor) — a clean
            // upgrade in every stat. "Stiller" trades LOWER raw damage (4 vs Bunker Emplacement's
            // 6) for much higher HP/armor/attack-speed, an ambiguous multi-stat trade — instead
            // it replaces Ballistics Analyst (0 damage, no granted ability, rating floor ~1),
            // where any real combatant is a guaranteed increase.
            var f9 = Swap(f8, P9, R1); // upgrade: hazmat vanguard -> "Vanquisher" Doctrine Tank
            var f10 = Swap(f9, P4, R2); // upgrade: ballistics analyst (0 dmg) -> "Stiller" Suppression Platform

            return new[]
            {
                Save(1, "Suppression Screen", "Infantry", f1),
                Save(2, "Assembly Line", "Infantry", f2),
                Save(3, "Ballistics Detail", "Support", f3),
                Save(4, "Hazmat Column", "Infantry", f4),
                Save(5, "Fire-Plan Advance", "Command", f5),
                Save(6, "Bunker Front", "Infantry", f6),
                Save(7, "Tankette Screen", "Vehicle", f7),
                Save(8, "Director's Program", "Boss", f8),
                Save(9, "The Vanquisher Deploys", "Boss", f9),
                Save(10, "Clinical Optimization", "Boss", f10)
            };
        }

        private static EnemyTemplateSO Save(int fight, string name, string tag, EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements, folder: Faction);
    }
}
