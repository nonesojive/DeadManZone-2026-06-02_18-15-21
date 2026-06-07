using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Aligns shop lane rows with the board grid top and zone strip bottom.
    /// </summary>
    public sealed class ShopLaneLayoutFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform offensiveRow;
        [SerializeField] private RectTransform defensiveRow;
        [SerializeField] private RectTransform specialtyRow;

        private int _applyPass;

        public void Configure(
            RectTransform offensive,
            RectTransform defensive,
            RectTransform specialty)
        {
            offensiveRow = offensive;
            defensiveRow = defensive;
            specialtyRow = specialty;
            _applyPass = 0;
            ApplyLayout();
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
            PositionLane(specialtyRow, 0);
            PositionLane(defensiveRow, 1);
            PositionLane(offensiveRow, 2);
        }

        private static void PositionLane(RectTransform row, int laneIndexFromBottom)
        {
            if (row == null)
                return;

            var (minY, maxY) = BuildLayoutMetrics.GetShopLaneAnchors(laneIndexFromBottom);
            row.anchorMin = new Vector2(0f, minY);
            row.anchorMax = new Vector2(BuildLayoutMetrics.ShopRightInset, maxY);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;
        }

        public static void EnsureOnShopArea(Transform shopArea)
        {
            if (shopArea == null)
                return;

            var fitter = shopArea.GetComponent<ShopLaneLayoutFitter>();
            if (fitter == null)
                fitter = shopArea.gameObject.AddComponent<ShopLaneLayoutFitter>();

            fitter.Configure(
                FindLaneRow(shopArea, "OffensiveRow"),
                FindLaneRow(shopArea, "DefensiveRow"),
                FindLaneRow(shopArea, "SpecialtyRow"));
        }

        private static RectTransform FindLaneRow(Transform parent, string rowName)
        {
            var row = parent.Find(rowName);
            return row != null ? row.GetComponent<RectTransform>() : null;
        }
    }
}
