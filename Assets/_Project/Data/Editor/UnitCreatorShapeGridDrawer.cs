using System.Collections.Generic;
using DeadManZone.Data.UnitCreation;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class UnitCreatorShapeGridDrawer
    {
        private const int GridRadius = 2;

        public static void Draw(UnitCreationDraft draft)
        {
            EditorGUILayout.LabelField("Shape Grid", EditorStyles.boldLabel);
            var cells = UnitCreatorShapePresets.ToCellSet(draft.shapeCells);

            EditorGUILayout.BeginVertical(GUILayout.Width(140));
            for (int y = GridRadius; y >= -GridRadius; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = -GridRadius; x <= GridRadius; x++)
                {
                    var coord = new Vector2Int(x, y);
                    bool active = cells.Contains(coord);
                    var style = active ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                    var label = active ? "■" : "·";
                    if (GUILayout.Button(label, style, GUILayout.Width(24), GUILayout.Height(24)))
                    {
                        if (active && cells.Count == 1)
                            continue;

                        if (active)
                            cells.Remove(coord);
                        else
                            cells.Add(coord);

                        draft.shapeCells = UnitCreatorShapePresets.FromCellSet(cells);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField($"Cells: {draft.shapeCells?.Length ?? 0}");
            if (draft.shapeCells != null)
            {
                foreach (var cell in draft.shapeCells)
                    EditorGUILayout.LabelField($"  ({cell.x}, {cell.y})");
            }
        }

        public static void DrawPresets(UnitCreationDraft draft)
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var preset in UnitCreatorShapePresets.All)
            {
                if (GUILayout.Button(preset.Label, EditorStyles.miniButton))
                    draft.shapeCells = (Vector2Int[])preset.Cells.Clone();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
