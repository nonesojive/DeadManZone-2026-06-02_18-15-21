using System.Linq;
using DeadManZone.Core;
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
            //
            // 2026-07-15 faction-roster-v1 Wave 2 (clean replace): crimson_legion/ash_wraiths
            // are retired — Crimson Assembly and Ashen Covenant replace them as the labeled
            // enemyFactionId. Their OWN new rosters (CrimsonAssemblyContentFactory/
            // AshenCovenantContentFactory) are produced by a completely separate pipeline
            // (AllFactionsContentFactory) that this legacy "5 Factions" DemoPieceFactory/
            // DemoEnemyFactory pair never calls, so referencing those new piece ids here would
            // throw a missing-key exception the moment this standalone menu runs on its own.
            // Nearest-role fallback instead reuses this same pipeline's OWN pieces (mirroring
            // BossRoster's identical "rifleman-fallback until the real content lands" pattern):
            // crimson_tank -> breakthrough_tank (tank), crimson_elite -> iron_guard (armored
            // line infantry), crimson_artillery -> grand_battery (artillery), wraith_stalker ->
            // marksman_doctrine_officer (stealth-tagged sniper, exact role match), wraith_phantom
            // -> sharpshooter (sniper), wraith_bombard -> toxin_launcher (Dust Scourge's own gas
            // hybrid — the only Gas-attack piece this pipeline creates, matching "Gas" fight tag).
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
                Save(5, "Crimson Assault", "Boss", FactionIds.CrimsonAssembly,
                    Placement(byId["breakthrough_tank"], 4, 4), Placement(byId["iron_guard"], 5, 4),
                    Placement(byId["grand_battery"], 0, 0)),
                Save(6, "Ash Phantoms", "Stealth", FactionIds.AshenCovenant,
                    Placement(byId["marksman_doctrine_officer"], 3, 4), Placement(byId["marksman_doctrine_officer"], 4, 4),
                    Placement(byId["sharpshooter"], 5, 4)),
                Save(7, "Neutral Battery", "Defense", "neutral",
                    Placement(byId["machine_gun_nest"], 3, 3), Placement(byId["militia_squad"], 4, 4),
                    Placement(byId["field_medic"], 4, 5)),
                Save(8, "Crimson Armor Push", "Mechanical", FactionIds.CrimsonAssembly,
                    Placement(byId["breakthrough_tank"], 4, 4), Placement(byId["breakthrough_tank"], 3, 5),
                    Placement(byId["iron_guard"], 5, 4)),
                Save(9, "Ash Chemical Front", "Gas", FactionIds.AshenCovenant,
                    Placement(byId["toxin_launcher"], 3, 3), Placement(byId["marksman_doctrine_officer"], 4, 4),
                    Placement(byId["sharpshooter"], 5, 4)),
                Save(10, "Final Gauntlet", "Boss", FactionIds.CrimsonAssembly,
                    Placement(byId["breakthrough_tank"], 4, 4), Placement(byId["grand_battery"], 1, 3),
                    Placement(byId["iron_guard"], 5, 4), Placement(byId["toxin_launcher"], 2, 3))
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
