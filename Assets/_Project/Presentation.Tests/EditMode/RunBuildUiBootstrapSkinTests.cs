using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Run;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Tests.EditMode
{
    /// <summary>M6 skin-only pass: the recolor that runs on the authored resource strip
    /// even when RunUiAuthoringLock preserves layout. Recolor only — never layout.</summary>
    public sealed class RunBuildUiBootstrapSkinTests
    {
        [Test]
        public void StyleResourceStrip_RecolorsBoxesAndLabels_WithoutTouchingLayout()
        {
            var strip = new GameObject("TopResourcePanel", typeof(RectTransform));

            try
            {
                // White sprite box with a value + green delta label (the authored look).
                var box = new GameObject("SuppliesBox", typeof(RectTransform), typeof(Image));
                box.transform.SetParent(strip.transform, false);
                box.GetComponent<Image>().color = Color.white;
                var boxRect = box.GetComponent<RectTransform>();
                boxRect.sizeDelta = new Vector2(120f, 40f);

                var value = NewLabel(box.transform, "SuppliesNumber", Color.black);
                var delta = NewLabel(box.transform, "SuppliesIncome", Color.green);

                // Button image/label must be left to the button skin pass.
                var button = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(strip.transform, false);
                button.GetComponent<Image>().color = Color.white;
                var buttonLabel = NewLabel(button.transform, "MenuLabel", Color.black);

                RunBuildUiBootstrap.StyleResourceStrip(strip.transform);

                Assert.AreEqual(CombatGrimdarkSkin.CardBody, box.GetComponent<Image>().color);
                Assert.AreEqual(CombatGrimdarkSkin.Bone, value.color);
                Assert.AreEqual(CombatGrimdarkSkin.BodyText, delta.color); // kit has no delta green
                Assert.AreEqual(Color.white, button.GetComponent<Image>().color);
                Assert.AreEqual(Color.black, buttonLabel.color);
                Assert.AreEqual(new Vector2(120f, 40f), boxRect.sizeDelta); // layout untouched
            }
            finally
            {
                Object.DestroyImmediate(strip);
            }
        }

        private static TMP_Text NewLabel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.color = color;
            return label;
        }
    }
}
