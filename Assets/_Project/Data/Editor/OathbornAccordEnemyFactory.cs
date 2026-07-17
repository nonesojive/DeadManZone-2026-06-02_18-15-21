using System.Linq;
using DeadManZone.Core;
using static DeadManZone.Data.Editor.EnemyTemplateAnchors;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): Oathborn Accord's 10-fight enemy ladder — shielded truncheon lines
    /// with a mercy sister in tow through fight 6 (the "shielded advances with medics" design
    /// spine), Confessor and Field Chirurgeon (Uncommon) debuting fights 5/7, then all three
    /// Rares land by fight 10: Armored Ark (fight 8, the transport tentpole), High Exarch
    /// (fight 9), Hospitaller-General (fight 10) — upgraded INTO existing medic/command slots
    /// rather than replacing the common medics, so the healing identity compounds instead of
    /// disappearing. Superset-growth per fight (see DustScourgeEnemyFactory's class doc).
    /// Pilgrim Spears (Triple3) is skipped — see EnemyTemplateAnchors' collision-avoidance note.
    /// </summary>
    internal static class OathbornAccordEnemyFactory
    {
        private const string Faction = FactionIds.OathbornAccord;

        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.Where(p => p.factionId == Faction).ToDictionary(p => p.id);
            var A = byId["truncheon_line"];
            var B = byId["vow_warden"];
            var C = byId["banner_bearer"];
            var D = byId["mercy_sister"];
            var U1 = byId["confessor"];
            var U2 = byId["field_chirurgeon"];
            var R1 = byId["armored_ark"];
            var R2 = byId["high_exarch"];
            var R3 = byId["hospitaller_general"];

            var f1 = new[] { Place(A, P6), Place(A, P8) };
            var f2 = f1.Append(Place(A, P5)).ToArray();
            var f3 = f2.Append(Place(D, P4)).ToArray();
            var f4 = f3.Append(Place(B, P9)).ToArray();
            var f5 = f4.Append(Place(U1, P7)).ToArray();
            var f6 = f5.Append(Place(C, P3)).ToArray();
            var f7 = f6.Append(Place(U2, P2)).ToArray();
            var f8 = f7.Append(Place(R1, P1)).ToArray(); // Armored Ark debut, 9 slots full
            var f9 = Swap(f8, P7, R2); // upgrade: Confessor -> High Exarch
            var f10 = Swap(f9, P2, R3); // upgrade: Field Chirurgeon -> Hospitaller-General

            return new[]
            {
                Save(1, "Peacekeeper Line", "Infantry", f1),
                Save(2, "Truncheon Column", "Infantry", f2),
                Save(3, "Mercy Detail", "Support", f3),
                Save(4, "Shield Wall Advance", "Infantry", f4),
                Save(5, "Confessor's Vigil", "Command", f5),
                Save(6, "Banner Held High", "Support", f6),
                Save(7, "Chirurgeon's Column", "Support", f7),
                Save(8, "The Armored Ark Marches", "Vehicle", f8),
                Save(9, "The Exarch's Crusade", "Boss", f9),
                Save(10, "The Accord's Full Muster", "Boss", f10)
            };
        }

        private static EnemyTemplateSO Save(int fight, string name, string tag, EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, Faction, placements, folder: Faction);
    }
}
