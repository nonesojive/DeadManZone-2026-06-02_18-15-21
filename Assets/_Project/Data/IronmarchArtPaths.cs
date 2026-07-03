namespace DeadManZone.Data
{
    /// <summary>Output paths for IronMarch Union themed art crops.</summary>
    public static class IronMarchArtPaths
    {
        public const string ArtRoot = "Assets/_Project/Art/IronMarch";
        public const string Icons = ArtRoot + "/Icons";
        public const string Cells = ArtRoot + "/Cells";
        public const string BoardRoot = "Assets/_Project/Art/Board/Themed";
        public const string CombatTiles = BoardRoot + "/Combat";
        public const string FrontTiles = BoardRoot + "/Front";
        public const string HqTiles = BoardRoot + "/Hq";
        public const string ReserveTiles = BoardRoot + "/Reserves";
        public const string PiecesRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";

        public static string IconAssetPath(string pieceId) => $"{Icons}/{pieceId}_icon.png";

        public static string CellAssetPath(string pieceId, string cellId) =>
            $"{Cells}/{pieceId}_{cellId}.png";

        public static readonly string[] PieceIds =
        {
            "supply_depot",
            "field_hospital",
            "officer_quarters",
            "command_outpost",
            "surgical_center",
            "recruitment_office",
            "field_medic",
            "conscript_rifleman",
            "armored_transport",
            "ironmarch_surgeon",
            "bulwark_squad",
            "enlisted_rifleman",
            "ironmarch_iron_horse",
            "ironclad_mortars",
            "ironclad_marksman",
            "ironclad_field_marshal",
            "machine_gun_nest"
        };
    }
}
