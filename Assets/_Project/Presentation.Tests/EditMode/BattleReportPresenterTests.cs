using DeadManZone.Core.Combat;
using DeadManZone.Presentation.Combat;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class BattleReportPresenterTests
    {
        [Test]
        public void Show_RendersRoutedLines_WhenUnitsRouted()
        {
            var (root, presenter, summary) = CreatePresenter();

            try
            {
                presenter.Show(new BattleReport
                {
                    PlayerWon = true,
                    ManpowerCasualties = 3,
                    SuppliesEarned = 12,
                    EnemyRouted = 2,
                    EnemyKilled = 4,
                    PlayerRouted = 1
                });

                StringAssert.Contains("Enemy broken: 2 routed / 4 killed", summary.text);
                StringAssert.Contains("Your routed units return next round (1)", summary.text);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Show_OmitsRoutedLines_WhenNothingRouted()
        {
            var (root, presenter, summary) = CreatePresenter();

            try
            {
                presenter.Show(new BattleReport
                {
                    PlayerWon = true,
                    ManpowerCasualties = 5,
                    SuppliesEarned = 8,
                    EnemyRouted = 0,
                    EnemyKilled = 3,
                    PlayerRouted = 0
                });

                StringAssert.DoesNotContain("routed", summary.text);
                StringAssert.Contains("Casualties: −5", summary.text);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static (GameObject root, BattleReportPresenter presenter, TMP_Text summary)
            CreatePresenter()
        {
            var root = new GameObject("BattleReport", typeof(RectTransform));
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);

            var presenter = root.AddComponent<BattleReportPresenter>();
            var summary = CreateText(panel.transform, "Summary");
            presenter.InitializeForTests(
                panel,
                CreateText(panel.transform, "Outcome"),
                summary,
                CreateText(panel.transform, "Dealt"),
                CreateText(panel.transform, "Taken"));

            return (root, presenter, summary);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }
    }
}
