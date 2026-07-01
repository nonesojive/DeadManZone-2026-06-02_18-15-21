using DeadManZone.Core.Board;

namespace DeadManZone.Data
{
    public static class SandboxArtRoster
    {
        public static readonly string[] AllPieceIds =
        {
            "conscript_rifleman", "grenade_thrower", "field_medic", "armored_transport",
            "mobile_cannon", "neutral_supply_depot", "neutral_field_gun", "shock_trooper",
            "neutral_mortar_team", "marksman_squad",
            "rifle_squad", "diesel_walker", "radio_array", "mg_team",
            "field_gun_nest", "supply_depot", "field_workshop", "mobile_artillery",
            "ironmarch_heavy_tank", "ironmarch_mortar", "ironmarch_engineer",
            "ironmarch_breacher", "ironmarch_sniper", "ironmarch_defender"
        };

        public static bool RequiresCombatArenaPrefab(PieceCategory category) =>
            category is PieceCategory.Unit or PieceCategory.Hybrid;
    }
}
