using System.Linq;
using DeadManZone.Core;
using DeadManZone.Data;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Authors the 10-fight gauntlet templates. 2026-07-12 balance pass: fights 1-2
    /// stay green patrols (no abilities on the field), fights 3+ anchor pieces so
    /// their fight-start auras actually fire — medics touch the line they heal,
    /// bulwarks pair up (Phalanx), the iron horse is hugged by infantry, and the
    /// field marshal stands beside enlisted riflemen (Command attack-speed aura).
    /// ArmyStrengthCalculator.EffectiveTotal rides a non-decreasing curve
    /// 138, 207, 266, 335, 408, 546, 606, 669, 735, 804 — deliberate knee at 5→6
    /// where the phalanx doctrine arrives in force (guarded by BalancePassTests).
    /// Anchors obey the 6x6 combat board: mg nest is 2x1 (x≤4), mortars 1x2 (y≤4),
    /// iron horse 3x2 (x≤3, y≤4) — EnemyTemplateSO.BuildBoard throws on overlap or
    /// out-of-bounds, so every comp here is placement-checked by the test suite.
    /// </summary>
    internal static class IronmarchEnemyFactory
    {
        private const string Faction = FactionIds.IronmarchUnion;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.ToDictionary(p => p.id);

            var conscript = byId["conscript_rifleman"];
            var medic = byId["field_medic"];
            var mgNest = byId["machine_gun_nest"];
            var enlisted = byId["enlisted_rifleman"];
            var bulwark = byId["bulwark_squad"];
            var mortars = byId["ironclad_mortars"];
            var ironHorse = byId["ironmarch_iron_horse"];
            var marksman = byId["ironclad_marksman"];
            var marshal = byId["ironclad_field_marshal"];

            return new[]
            {
                // 1-2: green patrols, no synergy pieces — the easy on-ramp.
                Save(1, "Conscript Patrol", "Infantry",
                    Placement(conscript, 4, 4), Placement(conscript, 5, 4)),
                Save(2, "Conscript Line", "Infantry",
                    Placement(conscript, 3, 4), Placement(conscript, 4, 4), Placement(conscript, 5, 4)),
                // 3: medic touches the line (+10 HP aura); nest dug in against it.
                Save(3, "Entrenched Clinic", "Support",
                    Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 4, 3)),
                // 4: full line, medic behind center, nest fronting the flank.
                Save(4, "Fortified Line", "Infantry",
                    Placement(conscript, 3, 4), Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 3, 3)),
                // 5: first true phalanx — bulwark pair buffs itself (+1 dmg / +5 HP each),
                // medic patches both the rifleman and the rear bulwark.
                Save(5, "Union Phalanx", "Infantry",
                    Placement(conscript, 3, 4), Placement(medic, 3, 5),
                    Placement(bulwark, 4, 4), Placement(bulwark, 4, 5),
                    Placement(enlisted, 5, 4), Placement(mgNest, 0, 4)),
                // 6: the knee. Phalanx pair entrenched with the nest, full conscript
                // wedge with medic and enlisted support.
                Save(6, "Bulwark Advance", "Mechanical",
                    Placement(conscript, 2, 4), Placement(conscript, 3, 4), Placement(conscript, 3, 3),
                    Placement(medic, 2, 5), Placement(enlisted, 3, 5),
                    Placement(bulwark, 4, 4), Placement(bulwark, 5, 4), Placement(mgNest, 4, 5)),
                // 7: armor debut — iron horse hugged by infantry (self +10 HP per
                // adjacent foot unit), medic patching the mortar crew.
                Save(7, "Iron Battery", "Artillery",
                    Placement(conscript, 4, 3), Placement(conscript, 5, 3), Placement(conscript, 5, 4),
                    Placement(enlisted, 5, 5), Placement(medic, 1, 4),
                    Placement(mgNest, 0, 5), Placement(mortars, 0, 3), Placement(ironHorse, 2, 4)),
                // 8: horse screened by the phalanx pair, infantry riding its flank.
                Save(8, "Iron Horse Push", "Vehicle",
                    Placement(conscript, 3, 3), Placement(conscript, 4, 3),
                    Placement(enlisted, 5, 3), Placement(bulwark, 5, 4), Placement(bulwark, 5, 5),
                    Placement(medic, 1, 4), Placement(mgNest, 0, 5),
                    Placement(mortars, 0, 3), Placement(ironHorse, 2, 4)),
                // 9: command debut — marshal beside the enlisted (Command attack-speed
                // aura + his +5 HP / +1 move), marksman spotting for the horse.
                Save(9, "Command Strike", "Boss",
                    Placement(conscript, 3, 4), Placement(conscript, 5, 4),
                    Placement(medic, 3, 5), Placement(enlisted, 4, 5), Placement(bulwark, 5, 5),
                    Placement(mgNest, 0, 4), Placement(mortars, 0, 0),
                    Placement(ironHorse, 0, 2), Placement(marksman, 3, 3), Placement(marshal, 4, 4)),
                // 10: everything at once — the fight-9 command wedge plus a third
                // conscript under the marshal's aura.
                Save(10, "Final March", "Boss",
                    Placement(conscript, 3, 4), Placement(conscript, 5, 4), Placement(conscript, 4, 3),
                    Placement(medic, 3, 5), Placement(enlisted, 4, 5), Placement(bulwark, 5, 5),
                    Placement(mgNest, 0, 4), Placement(mortars, 0, 0),
                    Placement(ironHorse, 0, 2), Placement(marksman, 3, 3), Placement(marshal, 4, 4))
            };
        }

        private static EnemyTemplateSO Save(
            int fight,
            string name,
            string tag,
            params EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements);

        private static EnemyPiecePlacement Placement(PieceDefinitionSO piece, int x, int y) =>
            DemoContentGenerator.Placement(piece, x, y);
    }
}
