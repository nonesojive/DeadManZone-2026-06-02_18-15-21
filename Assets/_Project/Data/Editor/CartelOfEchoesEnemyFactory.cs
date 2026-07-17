using System.Linq;
using DeadManZone.Core;
using static DeadManZone.Data.Editor.EnemyTemplateAnchors;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): Cartel of Echoes' 10-fight enemy ladder — PMC riflemen/strikebreakers
    /// through fight 6, Contract Officer (Uncommon command) debuting fight 5, Freelance Colonel
    /// (Rare) at fight 7, War Profiteer (Rare) at fight 9. Echo Chairman (a 0-damage command/
    /// economy piece) is left out of the AI ladder — see the f9 comment below for why swapping a
    /// real combatant for it measured as a net EffectiveTotal decrease despite higher raw rating.
    /// Superset-growth per fight (see DustScourgeEnemyFactory's class doc for the invariant this
    /// guarantees) — economy buildings (Freight Depot, Company Store, Executive Suite, Munitions
    /// Exchange) are never fielded; this is a fighting force, not a storefront.
    /// </summary>
    internal static class CartelOfEchoesEnemyFactory
    {
        private const string Faction = FactionIds.CartelOfEchoes;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.Where(p => p.factionId == Faction).ToDictionary(p => p.id);
            var A = byId["company_rifleman"];
            var B = byId["strikebreaker"];
            var C = byId["repo_crew"];
            var D = byId["paymasters_aide"];
            var U = byId["contract_officer"];
            var R1 = byId["freelance_colonel"];
            var R2 = byId["echo_chairman"];
            var R3 = byId["war_profiteer"];

            var f1 = new[] { Place(A, P6), Place(A, P8) };
            var f2 = f1.Append(Place(A, P5)).ToArray();
            var f3 = f2.Append(Place(B, P4)).ToArray();
            var f4 = f3.Append(Place(D, P9)).ToArray();
            var f5 = f4.Append(Place(U, P7)).ToArray();
            var f6 = f5.Append(Place(C, P3)).ToArray();
            var f7 = f6.Append(Place(R1, P2)).ToArray();
            var f8 = f7.Append(Place(C, P1)).ToArray();
            // f9 swap-safety (2026-07-17, caught by BalancePassTests — measured empirically, not
            // guessed): PieceCombatRating.ComputeBase alone is NOT a reliable predictor of a
            // swap's effect on ArmyStrengthCalculator.EffectiveTotal, because EffectiveTotal also
            // runs Critical Mass (board-wide tag-count thresholds) — swapping ONE piece for
            // another can cross a CM threshold the wrong way even when the raw piece rating goes
            // UP (repo_crew[108] -> war_profiteer[146] measured as a NET DECREASE in-board, 1158
            // -> 1104, despite +38 raw). The only swap proven safe in every case tried is
            // replacing a 0-baseDamage/no-ability piece (rated at a ~1 floor regardless of HP —
            // Paymaster's Aide has none of either) with a real combatant: a ~140-point raw jump
            // dwarfs any plausible CM swing. Echo Chairman (also 0 damage, a command/economy
            // piece per its own doc comment) has no safe real-piece target left to replace, so
            // it's left out of this ladder — Freelance Colonel + War Profiteer are enough
            // rare-signposts; fight 10 fields the same muster as fight 9 (still non-decreasing).
            var f9 = Swap(f8, P9, R3); // upgrade: paymaster's aide (0 dmg) -> War Profiteer
            var f10 = f9; // same muster — see f9 comment on why no further safe upgrade exists

            return new[]
            {
                Save(1, "Contract Detail", "PMC", f1),
                Save(2, "Retrieval Squad", "PMC", f2),
                Save(3, "Enforcement Line", "PMC", f3),
                Save(4, "Payroll Escort", "Support", f4),
                Save(5, "Contract Officer's Detail", "Command", f5),
                Save(6, "Full Muster, Billed Hourly", "PMC", f6),
                Save(7, "Freelance Command", "Command", f7),
                Save(8, "Cartel Task Force", "PMC", f8),
                Save(9, "War Profiteering", "Boss", f9),
                Save(10, "War Profiteering, Redoubled", "Boss", f10)
            };
        }

        private static EnemyTemplateSO Save(int fight, string name, string tag, EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements, folder: Faction);
    }
}
