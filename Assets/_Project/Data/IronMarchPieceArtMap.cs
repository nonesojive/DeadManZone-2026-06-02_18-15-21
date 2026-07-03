using UnityEngine;

namespace DeadManZone.Data
{
    public enum IronMarchIconSourceKind
    {
        SurvivalIcon,
        WastelandIcon,
        ArmyChess
    }

    /// <summary>Grid cell on a themed icon sheet for one piece icon or per-cell board crop.</summary>
    public readonly struct IronMarchSheetCell
    {
        public readonly string SheetPath;
        public readonly int Col;
        public readonly int Row;
        public readonly int GridCols;
        public readonly int GridRows;
        public readonly IronMarchIconSourceKind SourceKind;

        public IronMarchSheetCell(
            string sheetPath,
            int col,
            int row,
            int gridCols,
            int gridRows,
            IronMarchIconSourceKind sourceKind)
        {
            SheetPath = sheetPath;
            Col = col;
            Row = row;
            GridCols = gridCols;
            GridRows = gridRows;
            SourceKind = sourceKind;
        }
    }

    /// <summary>Maps each IronMarch piece to themed sheet crops (plan C).</summary>
    public static class IronMarchPieceArtMap
    {
        private const string Survival1 = "Assets/_Project/Art/Tilesets/survival icon/survival icon/1.png";
        private const string Survival2 = "Assets/_Project/Art/Tilesets/survival icon/survival icon/2.png";
        private const string Survival3 = "Assets/_Project/Art/Tilesets/survival icon/survival icon/3.png";
        private const string Wasteland1 = "Assets/_Project/Art/Tilesets/wasteland icons/1.png";
        private const string Wasteland2 = "Assets/_Project/Art/Tilesets/wasteland icons/2.png";
        private const string ArmyChess1 = "Assets/_Project/Art/Tilesets/army chess/tile-B-01.png";
        private const string ArmyChess2 = "Assets/_Project/Art/Tilesets/army chess/tile-B-02.png";

        private const int SurvivalGrid = 7;
        private const int WastelandGrid = 7;
        private const int ArmyGrid = 16;

        private static IronMarchSheetCell S(string path, int col, int row, IronMarchIconSourceKind kind) =>
            new(path, col, row, GridFor(kind), GridFor(kind), kind);

        private static int GridFor(IronMarchIconSourceKind kind) =>
            kind switch
            {
                IronMarchIconSourceKind.SurvivalIcon => SurvivalGrid,
                IronMarchIconSourceKind.WastelandIcon => WastelandGrid,
                IronMarchIconSourceKind.ArmyChess => ArmyGrid,
                _ => SurvivalGrid
            };

        public static bool TryGetIconCell(string pieceId, out IronMarchSheetCell cell)
        {
            if (IconByPieceId.TryGetValue(pieceId, out cell))
                return true;

            cell = default;
            return false;
        }

        public static bool TryGetCellSprite(string pieceId, Vector2Int localCell, out IronMarchSheetCell cell)
        {
            if (!CellBlocks.TryGetValue(pieceId, out var block))
            {
                if (!TryGetIconCell(pieceId, out cell))
                    return false;

                return true;
            }

            int col = block.Origin.Col + localCell.x;
            int row = block.Origin.Row + localCell.y;
            cell = new IronMarchSheetCell(
                block.Origin.SheetPath,
                col,
                row,
                block.Origin.GridCols,
                block.Origin.GridRows,
                block.Origin.SourceKind);
            return true;
        }

        public static string TryGetCombatSpritePath(string pieceId) =>
            CombatSpriteByPieceId.TryGetValue(pieceId, out var path) ? path : null;

        private readonly struct CellBlock
        {
            public readonly IronMarchSheetCell Origin;

            public CellBlock(IronMarchSheetCell origin) => Origin = origin;
        }

        private static readonly System.Collections.Generic.Dictionary<string, IronMarchSheetCell> IconByPieceId =
            new()
            {
                ["supply_depot"] = S(Survival1, 2, 4, IronMarchIconSourceKind.SurvivalIcon),
                ["field_hospital"] = S(Survival1, 4, 1, IronMarchIconSourceKind.SurvivalIcon),
                ["officer_quarters"] = S(Survival2, 1, 2, IronMarchIconSourceKind.SurvivalIcon),
                ["command_outpost"] = S(Survival1, 1, 3, IronMarchIconSourceKind.SurvivalIcon),
                ["surgical_center"] = S(Survival1, 4, 1, IronMarchIconSourceKind.SurvivalIcon),
                ["recruitment_office"] = S(Survival1, 2, 2, IronMarchIconSourceKind.SurvivalIcon),
                ["field_medic"] = S(Wasteland1, 5, 5, IronMarchIconSourceKind.WastelandIcon),
                ["conscript_rifleman"] = S(Wasteland1, 2, 3, IronMarchIconSourceKind.WastelandIcon),
                ["armored_transport"] = S(ArmyChess1, 2, 13, IronMarchIconSourceKind.ArmyChess),
                ["ironmarch_surgeon"] = S(Wasteland1, 6, 5, IronMarchIconSourceKind.WastelandIcon),
                ["bulwark_squad"] = S(Wasteland2, 2, 5, IronMarchIconSourceKind.WastelandIcon),
                ["enlisted_rifleman"] = S(Wasteland1, 1, 3, IronMarchIconSourceKind.WastelandIcon),
                ["ironmarch_iron_horse"] = S(ArmyChess1, 8, 3, IronMarchIconSourceKind.ArmyChess),
                ["ironclad_mortars"] = S(ArmyChess1, 5, 6, IronMarchIconSourceKind.ArmyChess),
                ["ironclad_marksman"] = S(Wasteland1, 0, 3, IronMarchIconSourceKind.WastelandIcon),
                ["ironclad_field_marshal"] = S(Survival3, 4, 4, IronMarchIconSourceKind.SurvivalIcon),
                ["machine_gun_nest"] = S(Wasteland1, 3, 6, IronMarchIconSourceKind.WastelandIcon)
            };

        private static readonly System.Collections.Generic.Dictionary<string, CellBlock> CellBlocks =
            new()
            {
                ["supply_depot"] = new(S(Survival1, 2, 4, IronMarchIconSourceKind.SurvivalIcon)),
                ["field_hospital"] = new(S(Survival1, 3, 1, IronMarchIconSourceKind.SurvivalIcon)),
                ["officer_quarters"] = new(S(Survival2, 0, 2, IronMarchIconSourceKind.SurvivalIcon)),
                ["command_outpost"] = new(S(Survival1, 1, 6, IronMarchIconSourceKind.SurvivalIcon)),
                ["ironmarch_iron_horse"] = new(S(ArmyChess1, 7, 2, IronMarchIconSourceKind.ArmyChess)),
                ["ironclad_mortars"] = new(S(ArmyChess1, 4, 6, IronMarchIconSourceKind.ArmyChess)),
                ["machine_gun_nest"] = new(S(Wasteland1, 3, 6, IronMarchIconSourceKind.WastelandIcon))
            };

        private static readonly System.Collections.Generic.Dictionary<string, string> CombatSpriteByPieceId =
            new()
            {
                ["supply_depot"] = "Assets/_Project/Art/Combat2D/Buildings/combat2d_building_supply_depot.png",
                ["field_medic"] = "Assets/_Project/Art/Combat2D/Units/Sprites/combat2d_unit_field_medic.png"
            };
    }
}
