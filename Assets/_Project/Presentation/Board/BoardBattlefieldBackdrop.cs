using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Full-bleed battlefield image aligned to the tile grid rect.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class BoardBattlefieldBackdrop : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private RectTransform gridRect;
        private void Awake()
        {
            if (image == null)
                image = GetComponent<Image>();

            if (image != null)
                image.raycastTarget = false;
        }

        public void Configure(RectTransform grid, Sprite backdrop)
        {
            gridRect = grid;
            if (image == null)
                image = GetComponent<Image>();

            if (image == null)
                return;

            image.sprite = backdrop;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.enabled = backdrop != null;
            image.color = Color.white;
            SyncToGrid();
        }

        public void SyncToGrid()
        {
            if (gridRect == null)
                return;

            var backdropRect = transform as RectTransform;
            if (backdropRect == null)
                return;

            backdropRect.anchorMin = gridRect.anchorMin;
            backdropRect.anchorMax = gridRect.anchorMax;
            backdropRect.pivot = gridRect.pivot;
            backdropRect.offsetMin = gridRect.offsetMin;
            backdropRect.offsetMax = gridRect.offsetMax;
        }
    }
}
