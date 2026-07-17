using System.Linq;
using DeadManZone.Core;
using static DeadManZone.Data.Editor.EnemyTemplateAnchors;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): Paradox Engine's 10-fight enemy ladder — chrono-fusiliers and phase
    /// vanguards through fight 6, Overclock Engineer and Resonance Coil (Uncommon) debuting
    /// fights 5/7, Doctor Recursion (Rare) at fight 8, Perpetual Engine (Rare, the one
    /// combat-board machine this roster fields) at fight 9, and a second Doctor Recursion at
    /// fight 10 — the tempo faction doubling its own recursion loop rather than reaching for a
    /// stat-ball finisher it doesn't have (only 2 of its 3 authored Rares — The Second Hand —
    /// is a pure HQ economy building and is skipped, same as every other faction's economy
    /// buildings). Superset-growth per fight (see DustScourgeEnemyFactory's class doc).
    /// </summary>
    internal static class ParadoxEngineEnemyFactory
    {
        private const string Faction = FactionIds.ParadoxEngine;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.Where(p => p.factionId == Faction).ToDictionary(p => p.id);
            var A = byId["chrono_fusilier"];
            var B = byId["phase_vanguard"];
            var C = byId["arc_lancer"];
            var D = byId["field_dynamo"];
            var U1 = byId["overclock_engineer"];
            var U2 = byId["resonance_coil"];
            var R1 = byId["doctor_recursion"];
            var R2 = byId["perpetual_engine"];

            var f1 = new[] { Place(A, P6), Place(A, P8) };
            var f2 = f1.Append(Place(A, P5)).ToArray();
            var f3 = f2.Append(Place(C, P4)).ToArray();
            var f4 = f3.Append(Place(D, P9)).ToArray();
            var f5 = f4.Append(Place(U1, P7)).ToArray();
            var f6 = f5.Append(Place(B, P3)).ToArray();
            var f7 = f6.Append(Place(U2, P2)).ToArray();
            var f8 = f7.Append(Place(R1, P1)).ToArray();
            // f9/f10 swap-safety (2026-07-17, caught by BalancePassTests via Cartel's identical
            // bug — see CartelOfEchoesEnemyFactory's comment): a 0-damage piece rates at a
            // floor of ~1 no matter its HP, so Field Dynamo (0 dmg) -> Perpetual Engine (0 dmg)
            // is a safe zero-for-zero swap, but the second Doctor Recursion copy (3 damage,
            // 35 HP) must replace ANOTHER real-but-weak combatant, not Chrono-Fusilier (6
            // damage, 50 HP, which is stronger) — Overclock Engineer (0 damage) is the correct
            // target: a real 3-damage piece strictly beats a 0-damage one.
            var f9 = Swap(f8, P9, R2); // upgrade: Field Dynamo (0 dmg) -> Perpetual Engine (0 dmg)
            var f10 = Swap(f9, P7, R1); // upgrade: Overclock Engineer (0 dmg) -> a second Doctor Recursion (3 dmg)

            return new[]
            {
                Save(1, "Chrono Patrol", "Infantry", f1),
                Save(2, "Fusilier Line", "Infantry", f2),
                Save(3, "Lancer Screen", "Infantry", f3),
                Save(4, "Dynamo Advance", "Support", f4),
                Save(5, "Overclocked Column", "Support", f5),
                Save(6, "Phase Wedge", "Infantry", f6),
                Save(7, "Echoing Line", "Support", f7),
                Save(8, "Recursion Debut", "Boss", f8),
                Save(9, "The Perpetual Engine", "Boss", f9),
                Save(10, "Recursive Doctrine", "Boss", f10)
            };
        }

        private static EnemyTemplateSO Save(int fight, string name, string tag, EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements, folder: Faction);
    }
}
