using System.Linq;
using DeadManZone.Core;
using static DeadManZone.Data.Editor.EnemyTemplateAnchors;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): Ashen Covenant's 10-fight enemy ladder — cheap fanatic bodies (ash
    /// acolytes, torchbearers) swarming from fight 1, Reliquary Bearer and Firebrand Vicar
    /// (Uncommon) debuting fights 5/7, Saint of the Embers (Rare) at fight 8, The Ash Martyr
    /// (Rare, fielded to be lost) at fight 9. Fight 10 fields the same host as fight 9 — see the
    /// f9 comment below for why a second Saint of the Embers copy isn't a safe further upgrade.
    /// Superset-growth per fight (see DustScourgeEnemyFactory's class doc). Zealot Mob (Triple3)
    /// is skipped — see EnemyTemplateAnchors' collision-avoidance note; Pyre Cathedral (a pure
    /// HQ economy building) is skipped too, same as every other faction's economy buildings.
    /// </summary>
    internal static class AshenCovenantEnemyFactory
    {
        private const string Faction = FactionIds.AshenCovenant;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.Where(p => p.factionId == Faction).ToDictionary(p => p.id);
            var A = byId["ash_acolyte"];
            var B = byId["torchbearer"];
            var C = byId["penitent"];
            var D = byId["hymnal_leader"];
            var U1 = byId["reliquary_bearer"];
            var U2 = byId["firebrand_vicar"];
            var R1 = byId["saint_of_the_embers"];
            var R2 = byId["the_ash_martyr"];

            var f1 = new[] { Place(A, P6), Place(A, P8) };
            var f2 = f1.Append(Place(A, P5)).ToArray();
            var f3 = f2.Append(Place(B, P4)).ToArray();
            var f4 = f3.Append(Place(D, P9)).ToArray();
            var f5 = f4.Append(Place(U1, P7)).ToArray();
            var f6 = f5.Append(Place(C, P3)).ToArray();
            var f7 = f6.Append(Place(U2, P2)).ToArray();
            var f8 = f7.Append(Place(R1, P1)).ToArray();
            // f9 swap-safety (2026-07-17, caught by BalancePassTests — measured empirically):
            // PieceCombatRating.ComputeBase is NOT a reliable predictor of a swap's effect on
            // ArmyStrengthCalculator.EffectiveTotal — Critical Mass (board-wide tag-count
            // thresholds) can cross a threshold the wrong way even when the raw piece rating
            // goes up (Cartel's repo_crew->war_profiteer swap measured as a net DECREASE despite
            // +38 raw — see CartelOfEchoesEnemyFactory's comment for the full finding). The only
            // swap proven safe is replacing a 0-baseDamage/no-ability piece (rated at a ~1 floor
            // regardless of HP — Hymnal Leader has none of either) with a real combatant: a
            // ~80-point raw jump dwarfs any plausible CM swing. Reliquary Bearer IS a real
            // combatant (rating 35, not near-zero), so it's not a safe swap target — a second
            // Saint of the Embers copy is left out of this ladder; fight 10 fields the same host
            // as fight 9 (still non-decreasing).
            var f9 = Swap(f8, P9, R2); // upgrade: Hymnal Leader (0 dmg) -> The Ash Martyr
            var f10 = f9; // same host — see f9 comment on why no further safe upgrade exists

            return new[]
            {
                Save(1, "Acolyte Mob", "Infantry", f1),
                Save(2, "Cinder Column", "Infantry", f2),
                Save(3, "Torchbearer Line", "Fire", f3),
                Save(4, "Hymnal Procession", "Support", f4),
                Save(5, "Reliquary Vigil", "Support", f5),
                Save(6, "Penitent Wedge", "Infantry", f6),
                Save(7, "Firebrand Column", "Fire", f7),
                Save(8, "The Saint's Ember", "Boss", f8),
                Save(9, "The Martyr's Vow", "Boss", f9),
                Save(10, "The Revolution of Cinders", "Boss", f10)
            };
        }

        private static EnemyTemplateSO Save(int fight, string name, string tag, EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements, folder: Faction);
    }
}
