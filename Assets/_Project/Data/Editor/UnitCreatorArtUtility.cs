using System.IO;
using DeadManZone.Data.UnitCreation;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class UnitCreatorArtUtility
    {
        public static void TryAutoAssignCellSprites(UnitCreationDraft draft)
        {
            if (draft?.shapeCells == null || string.IsNullOrWhiteSpace(draft.id))
                return;

            var entries = new System.Collections.Generic.List<PieceCellSprite>();
            foreach (var cell in draft.shapeCells)
            {
                var path = PieceArtPaths.CellAssetPath(draft.id, $"{cell.x}_{cell.y}");
                if (!File.Exists(path))
                    continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    continue;

                entries.Add(new PieceCellSprite { localCell = cell, sprite = sprite });
            }

            if (entries.Count > 0)
                draft.cellSprites = entries.ToArray();
        }
    }
}
