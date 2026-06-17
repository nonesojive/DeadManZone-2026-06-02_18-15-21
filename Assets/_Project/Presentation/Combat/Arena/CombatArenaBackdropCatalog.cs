using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>URP-safe Synty PolygonWar prefab paths for the combat arena backdrop rings.</summary>
    public static class CombatArenaBackdropCatalog
    {
        private const string WarEnv = "Assets/Synty/PolygonWar/Prefabs/Environments/";
        private const string WarBld = "Assets/Synty/PolygonWar/Prefabs/Buildings/";
        private const string WarProps = "Assets/Synty/PolygonWar/Prefabs/Props/";

        public static readonly string[] TrenchDressing =
        {
            WarEnv + "SM_Env_Sandbag_Wall_01.prefab",
            WarEnv + "SM_Env_Sandbag_Pile_01.prefab",
            WarEnv + "SM_Env_Rubble_Pile_01.prefab",
            WarEnv + "SM_Env_Rubble_Pile_Small.prefab",
            WarEnv + "SM_Env_Rubble_Stone_01.prefab",
            WarBld + "SM_Bld_Bunker_Wall_01.prefab",
            WarProps + "SM_Prop_Fence_Wire_01.prefab",
            WarProps + "SM_Prop_Wire_01.prefab"
        };

        public static readonly string[] Skyline =
        {
            WarBld + "SM_Bld_City_Destroyed_01.prefab",
            WarBld + "SM_Bld_City_Destroyed_02.prefab",
            WarBld + "SM_Bld_City_Destroyed_03.prefab",
            WarBld + "SM_Bld_Bunker_Large_Damaged_01.prefab",
            WarBld + "SM_Bld_Barn_Broken_01.prefab",
            WarBld + "SM_Bld_Guard_Booth_01.prefab"
        };

        public static readonly string[] AtmosphereFx = System.Array.Empty<string>();

        public static string[] TrenchDressingPaths => TrenchDressing;
        public static string[] SkylinePaths => Skyline;
        public static string[] AtmosphereFxPaths => AtmosphereFx;

        public static int GetPrefabCount(CombatArenaBackdropRing ring) => ring switch
        {
            CombatArenaBackdropRing.TrenchDressing => TrenchDressing.Length,
            CombatArenaBackdropRing.Skyline => Skyline.Length,
            CombatArenaBackdropRing.AtmosphereFx => AtmosphereFx.Length,
            _ => 0
        };

        public static string ResolvePath(CombatArenaBackdropRing ring, int catalogIndex)
        {
            string[] table = ring switch
            {
                CombatArenaBackdropRing.TrenchDressing => TrenchDressing,
                CombatArenaBackdropRing.Skyline => Skyline,
                CombatArenaBackdropRing.AtmosphereFx => AtmosphereFx,
                _ => TrenchDressing
            };

            if (table.Length == 0)
                return null;

            int index = catalogIndex % table.Length;
            if (index < 0)
                index += table.Length;

            return table[index];
        }
    }
}
