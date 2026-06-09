using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Renders a multi-cell piece footprint with per-cell fills and a centered label.
    /// </summary>
    public sealed class PieceShapeVisual : MonoBehaviour
    {
        public Vector2 Center => ((RectTransform)transform).localPosition;

        public static PieceShapeVisual Create(
            RectTransform overlay,
            GridLayoutGroup grid,
            IReadOnlyList<GridCoord> cells,
            PieceDefinition definition,
            PieceDefinitionSO source,
            GridCoord anchor,
            PieceRotation rotation,
            Func<GridCoord, Vector2?> cellCenterResolver = null)
        {
            if (overlay == null || grid == null || cells == null || cells.Count == 0 || definition == null)
                return null;

            var theme = UiThemeProvider.Current;
            var tint = PieceArtResolver.ResolveTint(definition, source, theme);
            var footprintBackground = PieceArtResolver.ResolveFootprintBackground(source, theme);
            bool useFootprintIcon = PieceArtResolver.ShouldUseFootprintIcon(source, anchor, rotation, definition);
            bool hideLabel = useFootprintIcon
                || PieceArtResolver.AllCellsHaveSprites(source, anchor, rotation, definition);

            var footprint = ComputeFootprint(overlay, grid, cells, cellCenterResolver);
            if (footprint.size.sqrMagnitude < 1f)
                return null;

            var root = new GameObject("PieceShape", typeof(RectTransform));
            root.transform.SetParent(overlay, false);
            var rootRect = root.GetComponent<RectTransform>();

            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = footprint.size;
            rootRect.localPosition = footprint.center;
            rootRect.localEulerAngles = Vector3.zero;

            PieceFootprintBackground.Create(rootRect, footprintBackground);
            PieceFootprintOutline.Create(rootRect);

            if (useFootprintIcon)
            {
                var artGo = new GameObject("FootprintIcon", typeof(RectTransform));
                artGo.transform.SetParent(root.transform, false);
                var artRect = artGo.GetComponent<RectTransform>();
                artRect.anchorMin = Vector2.zero;
                artRect.anchorMax = Vector2.one;
                artRect.offsetMin = new Vector2(1f, 1f);
                artRect.offsetMax = new Vector2(-1f, -1f);

                var artImage = artGo.AddComponent<Image>();
                artImage.sprite = source.icon;
                artImage.preserveAspect = true;
                artImage.raycastTarget = false;
                return root.AddComponent<PieceShapeVisual>();
            }

            float cellW = grid.cellSize.x;
            float cellH = grid.cellSize.y;

            foreach (var cell in cells)
            {
                var cellCenter = CellCenterInOverlay(overlay, grid, cell, cellCenterResolver);
                var block = new GameObject("Cell", typeof(RectTransform));
                block.transform.SetParent(root.transform, false);
                var blockRect = block.GetComponent<RectTransform>();
                blockRect.pivot = new Vector2(0.5f, 0.5f);
                blockRect.anchorMin = new Vector2(0.5f, 0.5f);
                blockRect.anchorMax = new Vector2(0.5f, 0.5f);
                blockRect.sizeDelta = new Vector2(cellW - 2f, cellH - 2f);
                blockRect.localPosition = cellCenter - footprint.center;

                var image = block.AddComponent<Image>();
                var localCell = PieceArtResolver.ToLocalCell(cell, anchor, rotation);
                var cellSprite = source?.TryGetCellSprite(localCell);
                if (cellSprite != null)
                {
                    image.sprite = cellSprite;
                    image.color = Color.white;
                }
                else
                {
                    image.color = Color.Lerp(tint, Color.white, 0.15f);
                }

                image.raycastTarget = false;
            }

            if (!hideLabel)
                AddFootprintLabel(root.transform, definition, footprint.size, theme);

            return root.AddComponent<PieceShapeVisual>();
        }

        private static void AddFootprintLabel(
            Transform parent,
            PieceDefinition definition,
            Vector2 footprintSize,
            UiThemeSO theme)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(parent, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(2f, 2f);
            labelRect.offsetMax = new Vector2(-2f, -2f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = GetShortName(definition);
            label.fontSize = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Min(footprintSize.x, footprintSize.y) * 0.22f),
                9,
                14);
            label.alignment = TextAlignmentOptions.Center;
            label.color = theme.textPrimary;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
        }

        private static (Vector2 center, Vector2 size) ComputeFootprint(
            RectTransform overlay,
            GridLayoutGroup grid,
            IReadOnlyList<GridCoord> cells,
            Func<GridCoord, Vector2?> cellCenterResolver = null)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float halfW = grid.cellSize.x * 0.5f;
            float halfH = grid.cellSize.y * 0.5f;

            foreach (var cell in cells)
            {
                var cellCenter = CellCenterInOverlay(overlay, grid, cell, cellCenterResolver);
                minX = Mathf.Min(minX, cellCenter.x - halfW);
                maxX = Mathf.Max(maxX, cellCenter.x + halfW);
                minY = Mathf.Min(minY, cellCenter.y - halfH);
                maxY = Mathf.Max(maxY, cellCenter.y + halfH);
            }

            var size = new Vector2(maxX - minX, maxY - minY);
            var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            return (center, size);
        }

        /// <summary>
        /// Center of a grid cell in overlay local space (overlay must match the TileGrid rect).
        /// </summary>
        internal static Vector2 CellCenterInOverlay(
            RectTransform overlay,
            GridLayoutGroup grid,
            GridCoord cell,
            Func<GridCoord, Vector2?> cellCenterResolver = null)
        {
            if (cellCenterResolver != null)
            {
                var resolved = cellCenterResolver(cell);
                if (resolved.HasValue)
                    return resolved.Value;
            }

            float strideX = grid.cellSize.x + grid.spacing.x;
            float strideY = grid.cellSize.y + grid.spacing.y;
            float left = -overlay.rect.width * overlay.pivot.x + grid.padding.left;
            float top = overlay.rect.height * (1f - overlay.pivot.y) - grid.padding.top;

            float x = left + cell.X * strideX + grid.cellSize.x * 0.5f;
            float y = top - cell.Y * strideY - grid.cellSize.y * 0.5f;
            return new Vector2(x, y);
        }

        private static string GetShortName(PieceDefinition definition)
        {
            if (!string.IsNullOrEmpty(definition.DisplayName))
                return definition.DisplayName;

            var id = definition.Id ?? string.Empty;
            return id.Length <= 14 ? id : id.Substring(0, 12) + "…";
        }
    }
}
