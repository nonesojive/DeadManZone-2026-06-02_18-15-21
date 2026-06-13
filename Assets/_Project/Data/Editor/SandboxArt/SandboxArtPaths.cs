namespace DeadManZone.Data.Editor
{
    internal static class SandboxArtPaths
    {
        internal const string CatalogAssetPath =
            "Assets/_Project/Data/Resources/DeadManZone/SandboxArtCatalog.asset";

        internal const string ResourcesFolder =
            "Assets/_Project/Data/Resources/DeadManZone";

        internal const string SandboxIconsFolder =
            "Assets/_Project/Art/Sandbox/Renders/Icons";

        internal const string ToonRoot =
            "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs";

        internal const string GermanInfantry = ToonRoot + "/TSww2_German_infantry.prefab";
        internal const string GermanMedic = ToonRoot + "/TSww2_German_medic.prefab";
        internal const string GermanSupport = ToonRoot + "/TSww2_German_support.prefab";
        internal const string GermanSniper = ToonRoot + "/TSww2_German_sniper.prefab";
        internal const string GermanOfficer = ToonRoot + "/TSww2_German_officer.prefab";

        internal const string AtvColor0 =
            "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/ATV_N1/0_Prefabs/ATV_N1_Color_0_Prefab.prefab";

        internal const string FaColor0 =
            "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/FA_N26/0_Prefabs/FA_N26_Color_0_Prefab.prefab";

        internal const string FaColor1 =
            "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/FA_N26/0_Prefabs/FA_N26_Color_1_Prefab.prefab";

        internal const string MshColor0 =
            "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/MSH_N2/0_Prefabs/MSH_N2_Color_0_Prefab.prefab";

        internal const string MshColor1 =
            "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/MSH_N2/0_Prefabs/MSH_N2_Color_1_Prefab.prefab";

        internal const string BuildingHq =
            "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_Hq.prefab";

        internal const string BuildingFieldGun =
            "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_FieldGun.prefab";

        internal const string BuildingSupplyDepot =
            "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_SupplyDepot.prefab";

        internal const string IconBunkerMap =
            "Assets/BunkerSurvivalUI/Sprites/Icons/icon_bunker_map.png";

        internal const string IconEmergencyRadio =
            "Assets/BunkerSurvivalUI/Sprites/Icons/icon_emergency_radio.png";

        internal const string IconFuelCanister =
            "Assets/BunkerSurvivalUI/Sprites/Icons/icon_fuel_canister.png";

        internal const string IconGeneratorPart =
            "Assets/BunkerSurvivalUI/Sprites/Icons/icon_generator_part.png";

        internal const string IconToolbox =
            "Assets/BunkerSurvivalUI/Sprites/Icons/icon_toolbox.png";

        internal static string GrokIcon(string pieceId) =>
            $"Assets/_Project/Art/Neutral/Renders/Icons/{pieceId}_icon.png";

        internal static string SandboxSnapshotIcon(string pieceId) =>
            $"{SandboxIconsFolder}/{pieceId}_icon.png";
    }
}
