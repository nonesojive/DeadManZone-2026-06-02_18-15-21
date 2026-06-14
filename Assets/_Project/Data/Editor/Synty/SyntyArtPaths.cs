namespace DeadManZone.Data.Editor
{
    internal static class SyntyArtPaths
    {
        internal const string ProjectArtRoot = "Assets/_Project/Art/Synty";
        internal const string ArenaUnits = ProjectArtRoot + "/Arena/Units";
        internal const string ArenaVehicles = ProjectArtRoot + "/Arena/Vehicles";
        internal const string ArenaBuildings = ProjectArtRoot + "/Arena/Buildings";
        internal const string Icons = ProjectArtRoot + "/Icons";

        internal const string SidekickRifle =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_03/ScifiSoldier_03.prefab";
        internal const string SidekickSupport =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_05/ScifiSoldier_05.prefab";
        internal const string SidekickMedic =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_02/ScifiSoldier_02.prefab";
        internal const string SidekickSniper =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_06/ScifiSoldier_06.prefab";
        internal const string SidekickOfficer =
            "Assets/Synty/SidekickCharacters/Characters/ScifiSoldiers/ScifiSoldier_04/ScifiSoldier_04.prefab";

        internal const string LocomotionController =
            "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/AC_Polygon_Masculine.controller";

        internal const string GermanTruck =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Truck_01.prefab";
        internal const string GermanCar =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Car_01.prefab";
        internal const string GermanHalftrack =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Halftrack_01.prefab";
        internal const string GermanTank =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_German_Tank_01.prefab";
        internal const string DieselWalkerSource =
            "Assets/Synty/PolygonWar/Prefabs/Vehicles/SM_Veh_Rocket_01.prefab";

        internal const string BunkerLarge =
            "Assets/Synty/PolygonWar/Prefabs/Buildings/SM_Bld_Bunker_Large_01.prefab";
        internal const string BunkerGun =
            "Assets/Synty/PolygonWar/Prefabs/Buildings/SM_Bld_Bunker_Gun_01.prefab";
        internal const string Barracks =
            "Assets/Synty/PolygonWar/Prefabs/Buildings/SM_Bld_Barracks_01.prefab";

        internal const string UnitRifle = ArenaUnits + "/ArenaUnit_Rifle.prefab";
        internal const string UnitSupport = ArenaUnits + "/ArenaUnit_Support.prefab";
        internal const string UnitMedic = ArenaUnits + "/ArenaUnit_Medic.prefab";
        internal const string UnitSniper = ArenaUnits + "/ArenaUnit_Sniper.prefab";
        internal const string UnitOfficer = ArenaUnits + "/ArenaUnit_Officer.prefab";
        internal const string VehicleTruck = ArenaVehicles + "/ArenaVehicle_Truck.prefab";
        internal const string VehicleCar = ArenaVehicles + "/ArenaVehicle_Car.prefab";
        internal const string VehicleHalftrack = ArenaVehicles + "/ArenaVehicle_Halftrack.prefab";
        internal const string VehicleTank = ArenaVehicles + "/ArenaVehicle_Tank.prefab";
        internal const string VehicleMech = ArenaVehicles + "/ArenaVehicle_Mech.prefab";
        internal const string BuildingHq = ArenaBuildings + "/ArenaBuilding_Hq.prefab";
        internal const string BuildingFieldGun = ArenaBuildings + "/ArenaBuilding_FieldGun.prefab";
        internal const string BuildingSupplyDepot = ArenaBuildings + "/ArenaBuilding_SupplyDepot.prefab";

        internal static string IconPath(string pieceId) => $"{Icons}/{pieceId}_icon.png";
    }
}
