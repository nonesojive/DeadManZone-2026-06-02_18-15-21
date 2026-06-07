using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Wires zone strip layout on scenes built before BoardZoneStripLayout existed.
    /// </summary>
    public static class BoardZoneStripBootstrap
    {
        private static readonly string[] ZoneNames = { "REAR", "SUPPORT", "FRONT" };

        public static void Ensure(BoardView boardView)
        {
            if (boardView == null || boardView.GetComponent<BoardZoneStripLayout>() != null)
                return;

            var grid = boardView.GridLayout;
            if (grid == null)
                return;

            var boardRect = boardView.GetComponent<RectTransform>();
            var gridRect = grid.GetComponent<RectTransform>();
            if (boardRect == null || gridRect == null)
                return;

            var strips = CollectStrips(boardRect);
            if (strips.Count < 3)
                return;

            var labels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
                labels[i] = EnsureStripLabel(strips[i], ZoneNames[i], boardRect, BoardZoneStripLayout.LegacyHeaderNames[i]);

            var layout = boardView.gameObject.AddComponent<BoardZoneStripLayout>();
            layout.Configure(
                boardRect,
                gridRect,
                grid,
                strips[0],
                strips[1],
                strips[2],
                labels[0],
                labels[1],
                labels[2],
                rearCols: 4,
                supportCols: 3);
        }

        private static List<RectTransform> CollectStrips(RectTransform boardRect)
        {
            var strips = new List<RectTransform>();
            for (int i = 0; i < boardRect.childCount; i++)
            {
                var child = boardRect.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                if (child.name is "REARStrip" or "SUPPORTStrip" or "FRONTStrip" or "ZoneStrip")
                    strips.Add(child);
            }

            return strips.OrderBy(s => s.anchorMin.x).ToList();
        }

        private static TMP_Text EnsureStripLabel(
            RectTransform strip,
            string zoneName,
            RectTransform boardRect,
            string legacyHeaderName)
        {
            var label = strip.GetComponentInChildren<TMP_Text>();
            if (label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(strip, false);
                var rect = labelGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                label = labelGo.AddComponent<TextMeshProUGUI>();
            }

            label.text = zoneName;
            label.fontSize = 12;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            var legacyHeader = boardRect.Find(legacyHeaderName);
            if (legacyHeader != null)
            {
                var headerLabel = legacyHeader.GetComponentInChildren<TMP_Text>();
                if (headerLabel != null)
                    label.color = headerLabel.color;
            }

            return label;
        }
    }
}
