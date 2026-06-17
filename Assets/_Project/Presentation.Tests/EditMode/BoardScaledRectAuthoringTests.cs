using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Run;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class BoardScaledRectAuthoringTests
    {
        [Test]
        public void ApplySize_SkipsResize_WhenPlayModeAuthoringPreserved()
        {
            var buildPanel = new GameObject("BuildPanel");
            buildPanel.AddComponent<RunUiAuthoringLock>();

            var sellZone = new GameObject("SellZone", typeof(RectTransform));
            sellZone.transform.SetParent(buildPanel.transform, false);
            var rect = sellZone.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 80f);
            sellZone.SetActive(false);

            var boardView = new GameObject("BoardView").AddComponent<BoardView>();
            var scaled = sellZone.AddComponent<BoardScaledRect>();
            scaled.SetLayoutForTests(boardView, 3, 3, 0.92f);

            try
            {
                scaled.ApplySizeForTests(simulatePlayMode: true);

                Assert.AreEqual(new Vector2(120f, 80f), rect.sizeDelta);
            }
            finally
            {
                Object.DestroyImmediate(boardView.gameObject);
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void ApplySize_Resizes_WhenNotInPlayMode()
        {
            var sellZone = new GameObject("SellZone", typeof(RectTransform));
            var rect = sellZone.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 80f);

            var boardView = new GameObject("BoardView").AddComponent<BoardView>();
            var scaled = sellZone.AddComponent<BoardScaledRect>();
            scaled.SetLayoutForTests(boardView, 1, 1, 1f);

            try
            {
                scaled.ApplySizeForTests(simulatePlayMode: false);

                Assert.AreNotEqual(new Vector2(120f, 80f), rect.sizeDelta);
            }
            finally
            {
                Object.DestroyImmediate(boardView.gameObject);
                Object.DestroyImmediate(sellZone);
            }
        }
    }
}
