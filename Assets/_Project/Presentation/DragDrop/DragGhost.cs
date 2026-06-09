using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class DragGhost : MonoBehaviour
    {
        private const float Padding = 4f;

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        private Transform _blockRoot;
        private string _pieceId;
        private PieceDefinition _definition;
        private float _cellSize = 36f;
        private float _cellSpacing = 3f;
        private bool _pieceOnly;

        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text;
        }

        public void SetRotation(PieceRotation rotation)
        {
            RebuildBlocks(rotation);
        }

        public static DragGhost Create(
            Transform parent,
            string pieceId,
            PieceDefinition definition = null,
            PieceRotation rotation = PieceRotation.R0,
            float cellSize = 36f,
            float cellSpacing = 3f,
            bool pieceOnly = false)
        {
            var theme = UiThemeProvider.Current;
            var source = PieceVisualLookup.GetSource(pieceId);
            var tint = definition != null
                ? theme.GetCategoryTint(definition.Category)
                : theme.cardColor;
            if (source != null && source.categoryTint.a > 0.01f)
                tint = source.categoryTint;

            var root = new GameObject("DragGhost", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);

            Image image = null;
            if (!pieceOnly)
            {
                image = root.AddComponent<Image>();
                image.color = Color.Lerp(theme.cardColor, tint, 0.35f);
                image.raycastTarget = false;
            }

            var blockRoot = new GameObject("Blocks", typeof(RectTransform));
            blockRoot.transform.SetParent(root.transform, false);
            var blockRootRect = blockRoot.GetComponent<RectTransform>();
            blockRootRect.anchorMin = Vector2.zero;
            blockRootRect.anchorMax = Vector2.one;
            blockRootRect.offsetMin = Vector2.zero;
            blockRootRect.offsetMax = Vector2.zero;

            TMP_Text tmp = null;
            if (!pieceOnly)
            {
                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(root.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(4f, 2f);
                textRect.offsetMax = new Vector2(-4f, -2f);

                tmp = textGo.AddComponent<TextMeshProUGUI>();
                if (definition != null && !string.IsNullOrEmpty(definition.DisplayName))
                    tmp.text = definition.DisplayName;
                else if (source != null && !string.IsNullOrEmpty(source.displayName))
                    tmp.text = source.displayName;
                else
                    tmp.text = pieceId ?? "piece";

                tmp.fontSize = 13;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = theme.textPrimary;
                tmp.raycastTarget = false;
            }

            var ghost = root.AddComponent<DragGhost>();
            ghost.background = image;
            ghost.label = tmp;
            ghost._blockRoot = blockRoot.transform;
            ghost._pieceId = pieceId;
            ghost._definition = definition;
            ghost._cellSize = cellSize;
            ghost._cellSpacing = cellSpacing;
            ghost._pieceOnly = pieceOnly;
            ghost.RebuildBlocks(rotation);
            return ghost;
        }

        private void RebuildBlocks(PieceRotation rotation)
        {
            if (_blockRoot == null)
                return;

            for (int i = _blockRoot.childCount - 1; i >= 0; i--)
                Destroy(_blockRoot.GetChild(i).gameObject);

            var theme = UiThemeProvider.Current;
            var source = PieceVisualLookup.GetSource(_pieceId);
            var tint = _definition != null
                ? PieceArtResolver.ResolveTint(_definition, source, theme)
                : theme.cardColor;

            var cells = _definition?.Shape != null
                ? _definition.Shape.GetCells(new GridCoord(0, 0), rotation).ToList()
                : new List<GridCoord> { new GridCoord(0, 0) };

            int minX = cells.Min(c => c.X);
            int maxX = cells.Max(c => c.X);
            int minY = cells.Min(c => c.Y);
            int maxY = cells.Max(c => c.Y);
            int footprintW = maxX - minX + 1;
            int footprintH = maxY - minY + 1;

            float stride = _cellSize + _cellSpacing;
            float pad = _pieceOnly ? Padding : Padding * 2f;
            var rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                footprintW * _cellSize + (footprintW - 1) * _cellSpacing + pad * 2f,
                footprintH * _cellSize + (footprintH - 1) * _cellSpacing + pad * 2f);
            rect.localEulerAngles = Vector3.zero;

            float footprintWpx = footprintW * _cellSize + (footprintW - 1) * _cellSpacing;
            float footprintHpx = footprintH * _cellSize + (footprintH - 1) * _cellSpacing;
            var blockRootRect = _blockRoot as RectTransform;
            var footprintBackground = PieceArtResolver.ResolveFootprintBackground(source, theme);
            if (blockRootRect != null)
            {
                PieceFootprintBackground.Create(
                    blockRootRect,
                    new Vector2(pad, -pad),
                    new Vector2(footprintWpx, footprintHpx),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    footprintBackground);

                PieceFootprintOutline.Create(
                    blockRootRect,
                    new Vector2(pad, -pad),
                    new Vector2(footprintWpx, footprintHpx),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));
            }

            if (PieceArtResolver.ShouldUseFootprintIcon(source, new GridCoord(0, 0), rotation, _definition))
            {
                var iconGo = new GameObject("FootprintIcon", typeof(RectTransform));
                iconGo.transform.SetParent(_blockRoot, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(2f, 2f);
                iconRect.offsetMax = new Vector2(-2f, -2f);

                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = source.icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                return;
            }

            var anchor = new GridCoord(0, 0);
            foreach (var cell in cells)
            {
                var block = new GameObject("Cell", typeof(RectTransform));
                block.transform.SetParent(_blockRoot, false);
                var blockRect = block.GetComponent<RectTransform>();
                blockRect.anchorMin = new Vector2(0f, 1f);
                blockRect.anchorMax = new Vector2(0f, 1f);
                blockRect.pivot = new Vector2(0.5f, 0.5f);
                blockRect.sizeDelta = new Vector2(_cellSize - 2f, _cellSize - 2f);
                blockRect.anchoredPosition = new Vector2(
                    pad + (cell.X - minX) * stride + _cellSize * 0.5f,
                    -(pad + (cell.Y - minY) * stride + _cellSize * 0.5f));

                var blockImage = block.AddComponent<Image>();
                var localCell = PieceArtResolver.ToLocalCell(cell, anchor, rotation);
                var cellSprite = source?.TryGetCellSprite(localCell);
                if (cellSprite != null)
                {
                    blockImage.sprite = cellSprite;
                    blockImage.color = Color.white;
                }
                else
                {
                    blockImage.color = Color.Lerp(tint, Color.white, 0.12f);
                }

                blockImage.raycastTarget = false;
            }
        }
    }
}
