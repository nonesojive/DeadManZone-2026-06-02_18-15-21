using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Tests
{
    public sealed class CombatHealthBarUiFactoryTests
    {
        [Test]
        public void CreateUnder_BuildsPlayerAndEnemyBars()
        {
            var panel = new GameObject("CombatPanel", typeof(RectTransform));
            try
            {
                var presenter = CombatHealthBarUiFactory.CreateUnder(panel.transform);

                Assert.NotNull(presenter);
                Assert.IsTrue(presenter.IsWired);
                Assert.NotNull(presenter.transform.Find("PlayerArmyBar"));
                Assert.NotNull(presenter.transform.Find("EnemyArmyBar"));
            }
            finally
            {
                Object.DestroyImmediate(panel);
            }
        }

        [Test]
        public void UsesSyntyBars_ReturnsFalseForLegacyFillRegionBar()
        {
            var panel = new GameObject("CombatPanel", typeof(RectTransform));
            var root = new GameObject("ArmyHealthBars", typeof(RectTransform));
            root.transform.SetParent(panel.transform, false);
            var bar = new GameObject("PlayerArmyBar", typeof(RectTransform));
            bar.transform.SetParent(root.transform, false);
            new GameObject("FillRegion", typeof(RectTransform)).transform.SetParent(bar.transform, false);
            var presenter = root.AddComponent<ArmyHealthBarPresenter>();

            Assert.IsFalse(CombatHealthBarUiFactory.UsesSyntyBars(presenter));
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void ArmyHealthBarView_SliderBinding_UpdatesDisplayedFraction()
        {
            var barRoot = new GameObject("Bar", typeof(RectTransform));
            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(barRoot.transform, false);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();

            var view = barRoot.AddComponent<ArmyHealthBarView>();
            view.BindSlider(slider);
            view.SetFractionImmediate(0.35f);

            Assert.AreEqual(0.35f, view.DisplayedFraction, 0.0001f);
            Object.DestroyImmediate(barRoot);
        }
    }
}
