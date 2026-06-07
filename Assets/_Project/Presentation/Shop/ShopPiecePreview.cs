using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Shop
{
    /// <summary>
    /// Renders a piece footprint at board cell scale inside a shop offer card square.
    /// </summary>
    public sealed class ShopPiecePreview : MonoBehaviour
    {
        [SerializeField] private RectTransform blockRoot;

        public void Initialize(RectTransform blocks)
        {
            blockRoot = blocks;
        }

        private void Awake()
        {
            ResolveBlockRoot();
        }

        public void Clear()
        {
            if (blockRoot == null)
                return;

            for (int i = blockRoot.childCount - 1; i >= 0; i--)
                Destroy(blockRoot.GetChild(i).gameObject);
        }

        public void Render(
            PieceDefinition definition,
            PieceDefinitionSO source,
            float cellSize,
            float spacing,
            float viewportSize = 0f)
        {
            ResolveBlockRoot();
            Clear();
            if (blockRoot == null || definition?.Shape == null)
                return;

            var cells = definition.Shape
                .GetCells(new GridCoord(0, 0), PieceRotation.R0)
                .ToList();
            if (cells.Count == 0)
                return;

            blockRoot.anchorMin = Vector2.zero;
            blockRoot.anchorMax = Vector2.one;
            blockRoot.offsetMin = Vector2.zero;
            blockRoot.offsetMax = Vector2.zero;

            float naturalViewport = ShopLayoutMetrics.IconAreaSize(cellSize, spacing);
            if (viewportSize < 1f)
                viewportSize = blockRoot.rect.height > 1f ? blockRoot.rect.height : naturalViewport;

            float scale = naturalViewport > 0.01f ? viewportSize / naturalViewport : 1f;
            float renderCell = cellSize * scale;
            float renderGap = spacing * scale;
            float stride = renderCell + renderGap;

            int minX = cells.Min(c => c.X);
            int maxX = cells.Max(c => c.X);
            int minY = cells.Min(c => c.Y);
            int maxY = cells.Max(c => c.Y);
            int footprintW = maxX - minX + 1;
            int footprintH = maxY - minY + 1;

            float footprintWpx = footprintW * renderCell + (footprintW - 1) * renderGap;
            float footprintHpx = footprintH * renderCell + (footprintH - 1) * renderGap;
            float offsetX = (viewportSize - footprintWpx) * 0.5f;
            float offsetY = (viewportSize - footprintHpx) * 0.5f;
            float half = viewportSize * 0.5f;

            if (source?.icon != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(blockRoot, false);
                iconGo.transform.SetAsFirstSibling();
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                float iconSize = viewportSize * 0.62f;
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = source.icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            var theme = UiThemeProvider.Current;
            var tint = PieceArtResolver.ResolveTint(definition, source, theme);
            var anchor = new GridCoord(0, 0);

            foreach (var cell in cells)
            {
                var block = new GameObject("Cell", typeof(RectTransform));
                block.transform.SetParent(blockRoot, false);
                var blockRect = block.GetComponent<RectTransform>();
                blockRect.anchorMin = new Vector2(0.5f, 0.5f);
                blockRect.anchorMax = new Vector2(0.5f, 0.5f);
                blockRect.pivot = new Vector2(0.5f, 0.5f);

                float blockInset = Mathf.Clamp(renderCell * 0.08f, 1f, 3f);
                float blockSize = Mathf.Max(renderCell - blockInset, 2f);
                blockRect.sizeDelta = new Vector2(blockSize, blockSize);

                float cx = offsetX + (cell.X - minX) * stride + renderCell * 0.5f;
                float cy = offsetY + (cell.Y - minY) * stride + renderCell * 0.5f;
                blockRect.anchoredPosition = new Vector2(cx - half, half - cy);

                var image = block.AddComponent<Image>();
                var localCell = PieceArtResolver.ToLocalCell(cell, anchor, PieceRotation.R0);
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

                var outline = block.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }

        private void ResolveBlockRoot()
        {
            if (blockRoot != null)
                return;

            var blocks = transform.Find("Blocks");
            blockRoot = blocks != null
                ? blocks.GetComponent<RectTransform>()
                : GetComponent<RectTransform>();
        }
    }
}
