using DeadManZone.Presentation.Run;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class ShopBackgroundBootstrapAuthoringTests
    {
        [Test]
        public void ApplyToBuildPanel_SkipsShopPanelMutation_WhenPlayModeAuthoringPreserved()
        {
            var buildPanel = CreateShopHierarchy(out var shopImage);
            buildPanel.AddComponent<RunUiAuthoringLock>();
            shopImage.sprite = Texture2D.whiteTexture != null ? Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f)) : null;
            shopImage.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);

            try
            {
                ShopBackgroundBootstrap.ApplyToBuildPanel(buildPanel.transform, null, simulatePlayMode: true);

                Assert.AreEqual(new Color(0.2f, 0.4f, 0.8f, 0.9f), shopImage.color);
                Assert.IsNotNull(shopImage.sprite);
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void ApplyToBuildPanel_ClearsShopPanel_WhenNotInPlayMode()
        {
            var buildPanel = CreateShopHierarchy(out var shopImage);
            shopImage.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);

            try
            {
                ShopBackgroundBootstrap.ApplyToBuildPanel(buildPanel.transform, null, simulatePlayMode: false);

                Assert.AreEqual(Color.clear, shopImage.color);
                Assert.IsNull(shopImage.sprite);
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }

        private static GameObject CreateShopHierarchy(out Image shopImage)
        {
            var buildPanel = new GameObject("BuildPanel");
            var mainRow = new GameObject("MainRow");
            mainRow.transform.SetParent(buildPanel.transform, false);

            var shopArea = new GameObject("ShopArea");
            shopArea.transform.SetParent(mainRow.transform, false);

            var shopPanel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            shopPanel.transform.SetParent(shopArea.transform, false);
            shopImage = shopPanel.GetComponent<Image>();
            return buildPanel;
        }
    }
}
