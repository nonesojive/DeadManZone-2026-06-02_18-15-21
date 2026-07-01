using System.Linq;
using DeadManZone.Core;
using DeadManZone.Data;

namespace DeadManZone.Data.Editor
{
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
                Save(1, "Conscript Patrol", "Infantry",
                    Placement(conscript, 4, 4), Placement(conscript, 5, 4)),
                Save(2, "Conscript Line", "Infantry",
                    Placement(conscript, 3, 4), Placement(conscript, 4, 4), Placement(conscript, 5, 4)),
                Save(3, "Entrenched Clinic", "Support",
                    Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 0, 4)),
                Save(4, "Fortified Line", "Infantry",
                    Placement(conscript, 3, 4), Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 0, 4)),
                Save(5, "Union Phalanx", "Infantry",
                    Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 0, 4),
                    Placement(enlisted, 3, 5), Placement(bulwark, 5, 5)),
                Save(6, "Bulwark Advance", "Mechanical",
                    Placement(conscript, 3, 4), Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 0, 4),
                    Placement(enlisted, 3, 5), Placement(bulwark, 5, 5)),
                Save(7, "Iron Battery", "Artillery",
                    Placement(conscript, 3, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 0, 4),
                    Placement(enlisted, 3, 5), Placement(bulwark, 5, 5),
                    Placement(mortars, 0, 0), Placement(ironHorse, 2, 0)),
                Save(8, "Iron Horse Push", "Vehicle",
                    Placement(conscript, 3, 4), Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 4, 5), Placement(mgNest, 0, 4),
                    Placement(enlisted, 3, 5), Placement(bulwark, 5, 5),
                    Placement(mortars, 0, 0), Placement(ironHorse, 2, 0)),
                Save(9, "Command Strike", "Boss",
                    Placement(conscript, 3, 4), Placement(conscript, 5, 4),
                    Placement(medic, 2, 5), Placement(mgNest, 0, 4),
                    Placement(enlisted, 3, 5), Placement(bulwark, 5, 5),
                    Placement(mortars, 0, 0), Placement(ironHorse, 2, 0),
                    Placement(marksman, 1, 5), Placement(marshal, 4, 4)),
                Save(10, "Final March", "Boss",
                    Placement(conscript, 3, 4), Placement(conscript, 4, 4), Placement(conscript, 5, 4),
                    Placement(medic, 2, 5), Placement(mgNest, 0, 4),
                    Placement(enlisted, 3, 5), Placement(bulwark, 5, 5),
                    Placement(mortars, 0, 0), Placement(ironHorse, 2, 0),
                    Placement(marksman, 4, 5), Placement(marshal, 2, 4))
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
