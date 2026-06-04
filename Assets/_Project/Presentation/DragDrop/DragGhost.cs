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
        private const float CellPixels = 36f;
        private const float Padding = 10f;

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        private Transform _blockRoot;
        private string _pieceId;
        private PieceDefinition _definition;

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
            PieceRotation rotation = PieceRotation.R0)
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

            var image = root.AddComponent<Image>();
            image.color = Color.Lerp(theme.cardColor, tint, 0.35f);
            image.raycastTarget = false;

            var blockRoot = new GameObject("Blocks", typeof(RectTransform));
            blockRoot.transform.SetParent(root.transform, false);
            var blockRootRect = blockRoot.GetComponent<RectTransform>();
            blockRootRect.anchorMin = Vector2.zero;
            blockRootRect.anchorMax = Vector2.one;
            blockRootRect.offsetMin = Vector2.zero;
            blockRootRect.offsetMax = Vector2.zero;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
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

            var ghost = root.AddComponent<DragGhost>();
            ghost.background = image;
            ghost.label = tmp;
            ghost._blockRoot = blockRoot.transform;
            ghost._pieceId = pieceId;
            ghost._definition = definition;
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
                ? theme.GetCategoryTint(_definition.Category)
                : theme.cardColor;
            if (source != null && source.categoryTint.a > 0.01f)
                tint = source.categoryTint;

            var cells = _definition?.Shape != null
                ? _definition.Shape.GetCells(new GridCoord(0, 0), rotation).ToList()
                : new List<GridCoord> { new GridCoord(0, 0) };

            int minX = cells.Min(c => c.X);
            int maxX = cells.Max(c => c.X);
            int minY = cells.Min(c => c.Y);
            int maxY = cells.Max(c => c.Y);
            int footprintW = maxX - minX + 1;
            int footprintH = maxY - minY + 1;

            var rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(
                footprintW * CellPixels + Padding * 2f,
                footprintH * CellPixels + Padding * 2f);
            rect.localEulerAngles = Vector3.zero;

            foreach (var cell in cells)
            {
                var block = new GameObject("Cell", typeof(RectTransform));
                block.transform.SetParent(_blockRoot, false);
                var blockRect = block.GetComponent<RectTransform>();
                blockRect.anchorMin = new Vector2(0f, 1f);
                blockRect.anchorMax = new Vector2(0f, 1f);
                blockRect.pivot = new Vector2(0.5f, 0.5f);
                blockRect.sizeDelta = new Vector2(CellPixels - 3f, CellPixels - 3f);
                blockRect.anchoredPosition = new Vector2(
                    Padding + (cell.X - minX) * CellPixels + CellPixels * 0.5f,
                    -(Padding + (cell.Y - minY) * CellPixels + CellPixels * 0.5f));

                var blockImage = block.AddComponent<Image>();
                blockImage.color = Color.Lerp(tint, Color.white, 0.12f);
                blockImage.raycastTarget = false;

                var outline = block.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }
    }
}
