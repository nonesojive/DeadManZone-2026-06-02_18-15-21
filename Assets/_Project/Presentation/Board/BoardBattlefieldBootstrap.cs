using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Ensures battlefield backdrop and grid overlay exist on older board layouts.
    /// </summary>
    public static class BoardBattlefieldBootstrap
    {
        public static (BoardBattlefieldBackdrop backdrop, BoardGridOverlay overlay) Ensure(
            BoardView boardView,
            RectTransform boardRect,
            RectTransform gridRect,
            GridLayoutGroup grid)
        {
            if (boardView == null || boardRect == null || gridRect == null || grid == null)
                return (null, null);

            var backdrop = EnsureBackdrop(boardRect, gridRect);
            var overlay = EnsureOverlay(boardRect, gridRect, grid);

            int gridIndex = gridRect.GetSiblingIndex();
            backdrop.transform.SetSiblingIndex(gridIndex);
            overlay.transform.SetSiblingIndex(gridIndex + 2);
            return (backdrop, overlay);
        }

        private static BoardBattlefieldBackdrop EnsureBackdrop(RectTransform boardRect, RectTransform gridRect)
        {
            var existing = boardRect.Find("BattlefieldBackdrop");
            if (existing != null)
                return existing.GetComponent<BoardBattlefieldBackdrop>()
                    ?? existing.gameObject.AddComponent<BoardBattlefieldBackdrop>();

            var go = new GameObject("BattlefieldBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(boardRect, false);
            var backdrop = go.AddComponent<BoardBattlefieldBackdrop>();
            backdrop.Configure(gridRect, null);
            return backdrop;
        }

        private static BoardGridOverlay EnsureOverlay(
            RectTransform boardRect,
            RectTransform gridRect,
            GridLayoutGroup grid)
        {
            var existing = boardRect.Find("GridOverlay");
            if (existing != null)
                return existing.GetComponent<BoardGridOverlay>()
                    ?? existing.gameObject.AddComponent<BoardGridOverlay>();

            var go = new GameObject("GridOverlay", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(boardRect, false);
            return go.AddComponent<BoardGridOverlay>();
        }
    }
}
