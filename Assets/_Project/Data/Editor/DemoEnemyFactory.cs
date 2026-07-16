using System.Linq;
using DeadManZone.Data;

namespace DeadManZone.Data.Editor
{
    internal static class DemoEnemyFactory
    {
        public static EnemyTemplateSO[] CreateAll(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.ToDictionary(p => p.id);

            // 2026-07-15 faction-roster-v1: "Neutral Armor"/"Neutral Battery" used the old
            // Neutral rares (armored_transport/mobile_cannon/grenade_thrower), all deleted —
            // §2.1 gives Neutral no vehicles and no rares. Substituted with the nearest
            // surviving/new Neutral pieces (smallest-diff — see agent report).
            return new[]
            {
                Save(1, "Conscript Line", "Infantry", "neutral",
                    Placement(byId["militia_squad"], 4, 4)),
                Save(2, "Patrol", "Infantry", "neutral",
                    Placement(byId["militia_squad"], 4, 4), Placement(byId["field_medic"], 5, 4)),
                Save(3, "Field Support", "Infantry", "neutral",
                    Placement(byId["militia_squad"], 4, 4), Placement(byId["militia_squad"], 5, 4)),
                Save(4, "Neutral Redoubt", "Defense", "neutral",
                    Placement(byId["machine_gun_nest"], 3, 4), Placement(byId["trench_works"], 1, 1)),
                Save(5, "Crimson Assault", "Boss", "crimson_legion",
                    Placement(byId["crimson_tank"], 4, 4), Placement(byId["crimson_elite"], 5, 4),
                    Placement(byId["crimson_artillery"], 0, 0)),
                Save(6, "Ash Phantoms", "Stealth", "ash_wraiths",
                    Placement(byId["wraith_stalker"], 3, 4), Placement(byId["wraith_stalker"], 4, 4),
                    Placement(byId["wraith_phantom"], 5, 4)),
                Save(7, "Neutral Battery", "Defense", "neutral",
                    Placement(byId["machine_gun_nest"], 3, 3), Placement(byId["militia_squad"], 4, 4),
                    Placement(byId["field_medic"], 4, 5)),
                Save(8, "Crimson Armor Push", "Mechanical", "crimson_legion",
                    Placement(byId["crimson_tank"], 4, 4), Placement(byId["crimson_tank"], 3, 5),
                    Placement(byId["crimson_elite"], 5, 4)),
                Save(9, "Ash Chemical Front", "Gas", "ash_wraiths",
                    Placement(byId["wraith_bombard"], 3, 3), Placement(byId["wraith_stalker"], 4, 4),
                    Placement(byId["wraith_phantom"], 5, 4)),
                Save(10, "Final Gauntlet", "Boss", "crimson_legion",
                    Placement(byId["crimson_tank"], 4, 4), Placement(byId["crimson_artillery"], 1, 3),
                    Placement(byId["crimson_elite"], 5, 4), Placement(byId["wraith_bombard"], 2, 3))
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
