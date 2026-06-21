using DeadManZone.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Reserves
{
    [RequireComponent(typeof(Image))]
    public sealed class ReservesTileView : MonoBehaviour
    {
        [SerializeField] private Image baseImage;

        public GridCoord Coord { get; private set; }
        public string OccupyingInstanceId { get; private set; }

        private void Awake()
        {
            if (baseImage == null)
                baseImage = GetComponent<Image>();
            if (baseImage != null)
                baseImage.raycastTarget = true;
        }

        public void Initialize(GridCoord coord, Color baseColor, Sprite slotSprite = null)
        {
            Coord = coord;
            ApplySlotVisual(slotSprite, baseColor);
        }

        public void ApplySlotVisual(Sprite slotSprite, Color fallbackColor)
        {
            if (baseImage == null)
                return;

            if (slotSprite != null)
            {
                baseImage.sprite = slotSprite;
                baseImage.type = Image.Type.Simple;
                baseImage.preserveAspect = false;
                baseImage.color = Color.white;
                return;
            }

            baseImage.color = fallbackColor;
        }

        public void SetOccupied(string instanceId, bool occupied) =>
            OccupyingInstanceId = occupied ? instanceId : null;
    }
}
