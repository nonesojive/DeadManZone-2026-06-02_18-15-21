using DeadManZone.Core.Shop;
using DeadManZone.Presentation.Shop;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class ShopOfferViewTests
    {
        [Test]
        public void ConfigureLayout_AppliesRuntimeSizing_WhenRefsAreWired()
        {
            var root = new GameObject("ShopOfferCard", typeof(RectTransform));
            try
            {
                var cardRect = root.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(120f, 180f);

                var squareGo = new GameObject("SquareRoot", typeof(RectTransform));
                squareGo.transform.SetParent(root.transform, false);
                var squareRect = squareGo.GetComponent<RectTransform>();
                squareRect.sizeDelta = new Vector2(90f, 90f);

                var nameGo = new GameObject("PieceIdText", typeof(TextMeshProUGUI));
                nameGo.transform.SetParent(root.transform, false);

                var view = root.AddComponent<ShopOfferView>();
                view.InitializeForTests(squareRect, nameGo.GetComponent<TextMeshProUGUI>(), cardRect);

                view.ConfigureLayout(24f, 2f, 400f, 300f, 6);

                Assert.AreNotEqual(120f, cardRect.sizeDelta.x);
                Assert.AreNotEqual(180f, cardRect.sizeDelta.y);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
