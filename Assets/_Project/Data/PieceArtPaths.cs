namespace DeadManZone.Data
{
    /// <summary>
    /// Conventional asset paths for the neutral Blender → Unity art pipeline.
    /// </summary>
    public static class PieceArtPaths
    {
        public const string ArtRoot = "Assets/_Project/Art";
        public const string NeutralRoot = ArtRoot + "/Neutral";
        public const string NeutralSource = NeutralRoot + "/Source";
        public const string NeutralStyleSheet = NeutralRoot + "/StyleSheet";
        public const string NeutralIcons = NeutralRoot + "/Renders/Icons";
        public const string NeutralCells = NeutralRoot + "/Renders/Cells";
        public const string SharedRoot = ArtRoot + "/Shared";

        public static string IconFileName(string pieceId) => $"{pieceId}_icon.png";

        public static string IconAssetPath(string pieceId) =>
            $"{NeutralIcons}/{IconFileName(pieceId)}";

        public static string CellAssetPath(string pieceId, string cellId) =>
            $"{NeutralCells}/{pieceId}_{cellId}.png";

        public static readonly string[] NeutralPieceIds =
        {
            "conscript_rifleman",
            "grenade_thrower",
            "field_medic",
            "armored_transport",
            "mobile_cannon"
        };
    }
}
