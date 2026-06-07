using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Aligns zone color strips and labels with live grid column boundaries.
    /// </summary>
    public sealed class BoardZoneStripLayout : MonoBehaviour
    {
        public static readonly string[] LegacyHeaderNames = { "REARHeader", "SUPPORTHeader", "FRONTHeader" };

        [SerializeField] private RectTransform boardRect;
        [SerializeField] private RectTransform gridRect;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private RectTransform rearStrip;
        [SerializeField] private RectTransform supportStrip;
        [SerializeField] private RectTransform frontStrip;
        [SerializeField] private TMP_Text rearLabel;
        [SerializeField] private TMP_Text supportLabel;
        [SerializeField] private TMP_Text frontLabel;
        [SerializeField] private int rearColumns = 4;
        [SerializeField] private int supportColumns = 3;
        [SerializeField] private float stripMinY = BuildLayoutMetrics.ZoneStripMinY;
        [SerializeField] private float stripMaxY = BuildLayoutMetrics.ZoneStripMaxY;

        private int _applyPass;

        public void Configure(
            RectTransform board,
            RectTransform gridRoot,
            GridLayoutGroup gridLayout,
            RectTransform rear,
            RectTransform support,
            RectTransform front,
            TMP_Text rearText,
            TMP_Text supportText,
            TMP_Text frontText,
            int rearCols,
            int supportCols)
        {
            boardRect = board;
            gridRect = gridRoot;
            grid = gridLayout;
            rearStrip = rear;
            supportStrip = support;
            frontStrip = front;
            rearLabel = rearText;
            supportLabel = supportText;
            frontLabel = frontText;
            rearColumns = rearCols;
            supportColumns = supportCols;
            _applyPass = 0;
            ApplyLayout();
            RemoveLegacyHeaders(boardRect);
        }

        private void OnEnable()
        {
            _applyPass = 0;
            ApplyLayout();
        }

        private void LateUpdate()
        {
            if (_applyPass >= 2)
                return;

            ApplyLayout();
            _applyPass++;
        }

        private void OnRectTransformDimensionsChange()
        {
            _applyPass = 0;
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            if (boardRect == null || gridRect == null || grid == null)
                return;

            Canvas.ForceUpdateCanvases();

            int frontStart = rearColumns + supportColumns;
            int totalColumns = grid.constraintCount;
            if (totalColumns <= 0)
                totalColumns = frontStart + Mathf.Max(1, totalColumns - frontStart);

            PositionStrip(rearStrip, 0, rearColumns - 1);
            PositionStrip(supportStrip, rearColumns, rearColumns + supportColumns - 1);
            PositionStrip(frontStrip, frontStart, totalColumns - 1);
        }

        private void PositionStrip(RectTransform strip, int colStart, int colEnd)
        {
            if (strip == null || colEnd < colStart)
                return;

            float leftLocal = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, colStart, left: true);
            float rightLocal = GridLayoutColumnMetrics.GetColumnEdgeLocal(gridRect, grid, colEnd, left: false);

            var worldLeft = gridRect.TransformPoint(new Vector3(leftLocal, gridRect.rect.yMin, 0f));
            var worldRight = gridRect.TransformPoint(new Vector3(rightLocal, gridRect.rect.yMin, 0f));
            var boardLeft = boardRect.InverseTransformPoint(worldLeft);
            var boardRight = boardRect.InverseTransformPoint(worldRight);

            float boardWidth = boardRect.rect.width;
            if (boardWidth <= 1f)
                return;

            float minX = (boardLeft.x - boardRect.rect.xMin) / boardWidth;
            float maxX = (boardRight.x - boardRect.rect.xMin) / boardWidth;

            strip.anchorMin = new Vector2(minX, stripMinY);
            strip.anchorMax = new Vector2(maxX, stripMaxY);
            strip.offsetMin = Vector2.zero;
            strip.offsetMax = Vector2.zero;
        }

        public static void RemoveLegacyHeaders(Transform boardRect)
        {
            foreach (var name in LegacyHeaderNames)
            {
                Transform header = boardRect != null ? boardRect.Find(name) : null;
                if (header == null)
                {
                    var orphan = GameObject.Find(name);
                    if (orphan != null)
                        header = orphan.transform;
                }

                if (header == null)
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(header.gameObject);
                else
#endif
                    Object.Destroy(header.gameObject);
            }
        }
    }
}
