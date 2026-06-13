using System.Linq;
using DeadManZone.Data;

namespace DeadManZone.Data.Editor
{
    internal static class DemoEnemyFactory
    {
        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.ToDictionary(p => p.id);

            return new[]
            {
                Save(1, "Conscript Line", "Infantry", "neutral",
                    Placement(byId["conscript_rifleman"], 4, 4)),
                Save(2, "Patrol", "Infantry", "neutral",
                    Placement(byId["conscript_rifleman"], 4, 4), Placement(byId["field_medic"], 5, 4)),
                Save(3, "Field Support", "Infantry", "neutral",
                    Placement(byId["conscript_rifleman"], 4, 4), Placement(byId["conscript_rifleman"], 5, 4)),
                Save(4, "Neutral Armor", "Vehicle", "neutral",
                    Placement(byId["armored_transport"], 6, 4), Placement(byId["mobile_cannon"], 4, 1)),
                Save(5, "Crimson Assault", "Boss", "crimson_legion",
                    Placement(byId["crimson_tank"], 7, 4), Placement(byId["crimson_elite"], 5, 4),
                    Placement(byId["crimson_artillery"], 0, 0)),
                Save(6, "Ash Phantoms", "Stealth", "ash_wraiths",
                    Placement(byId["wraith_stalker"], 6, 4), Placement(byId["wraith_stalker"], 4, 4),
                    Placement(byId["wraith_phantom"], 8, 4)),
                Save(7, "Neutral Battery", "Artillery", "neutral",
                    Placement(byId["mobile_cannon"], 6, 3), Placement(byId["grenade_thrower"], 5, 4),
                    Placement(byId["field_medic"], 5, 6)),
                Save(8, "Crimson Armor Push", "Mechanical", "crimson_legion",
                    Placement(byId["crimson_tank"], 7, 4), Placement(byId["crimson_tank"], 5, 5),
                    Placement(byId["crimson_elite"], 4, 4)),
                Save(9, "Ash Chemical Front", "Gas", "ash_wraiths",
                    Placement(byId["wraith_bombard"], 6, 3), Placement(byId["wraith_stalker"], 4, 4),
                    Placement(byId["wraith_phantom"], 8, 4)),
                Save(10, "Final Gauntlet", "Boss", "crimson_legion",
                    Placement(byId["crimson_tank"], 7, 4), Placement(byId["crimson_artillery"], 1, 3),
                    Placement(byId["crimson_elite"], 5, 4), Placement(byId["wraith_bombard"], 4, 3))
            };
        }

        private static EnemyTemplateSO Save(
            int fight,
            string name,
            string tag,
            string factionId,
            params EnemyPiecePlacement[] placements) =>
            DemoContentGenerator.SaveEnemy(fight, name, tag, factionId, placements);

        private static EnemyPiecePlacement Placement(PieceDefinitionSO piece, int x, int y) =>
            DemoContentGenerator.Placement(piece, x, y);
    }
}
