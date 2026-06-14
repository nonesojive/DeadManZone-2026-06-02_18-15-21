namespace DeadManZone.Data.Editor
{
    internal static class SandboxArtPaths
    {
        internal const string CatalogAssetPath =
            "Assets/_Project/Data/Resources/DeadManZone/SandboxArtCatalog.asset";

        internal const string ResourcesFolder =
            "Assets/_Project/Data/Resources/DeadManZone";

        internal const string SandboxIconsFolder = SyntyArtPaths.Icons;

        internal const string GermanInfantry = SyntyArtPaths.UnitRifle;
        internal const string GermanMedic = SyntyArtPaths.UnitMedic;
        internal const string GermanSupport = SyntyArtPaths.UnitSupport;
        internal const string GermanSniper = SyntyArtPaths.UnitSniper;
        internal const string GermanOfficer = SyntyArtPaths.UnitOfficer;

        internal const string AtvColor0 = SyntyArtPaths.VehicleTruck;
        internal const string FaColor0 = SyntyArtPaths.VehicleMech;
        internal const string FaColor1 = SyntyArtPaths.VehicleTank;
        internal const string MshColor0 = SyntyArtPaths.VehicleCar;
        internal const string MshColor1 = SyntyArtPaths.VehicleHalftrack;

        internal const string BuildingHq = SyntyArtPaths.BuildingHq;
        internal const string BuildingFieldGun = SyntyArtPaths.BuildingFieldGun;
        internal const string BuildingSupplyDepot = SyntyArtPaths.BuildingSupplyDepot;

        internal static string GrokIcon(string pieceId) => SyntyArtPaths.IconPath(pieceId);

        internal static string SandboxSnapshotIcon(string pieceId) => SyntyArtPaths.IconPath(pieceId);
    }
}
