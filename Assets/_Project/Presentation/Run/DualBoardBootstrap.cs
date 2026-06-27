using DeadManZone.Core.Board;
using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Runtime setup for side-by-side combat and HQ board views.</summary>
    public static class DualBoardBootstrap
    {
        public static BoardView EnsureHqBoardView(BoardView combatBoardView, Transform boardArea)
        {
            if (combatBoardView == null || boardArea == null)
                return null;

            var existing = boardArea.Find("HqBoardView");
            if (existing != null && existing.TryGetComponent<BoardView>(out var hqView))
            {
                hqView.SetBoardBinding(BoardKind.Hq);
                return hqView;
            }

            var clone = Object.Instantiate(combatBoardView.gameObject, boardArea);
            clone.name = "HqBoardView";
            var rect = clone.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var combatRect = combatBoardView.GetComponent<RectTransform>();
            if (combatRect != null)
            {
                combatRect.anchorMin = new Vector2(0f, 0f);
                combatRect.anchorMax = new Vector2(0.5f, 1f);
                combatRect.offsetMin = Vector2.zero;
                combatRect.offsetMax = Vector2.zero;
            }

            hqView = clone.GetComponent<BoardView>();
            hqView.SetBoardBinding(BoardKind.Hq);
            return hqView;
        }
    }
}
