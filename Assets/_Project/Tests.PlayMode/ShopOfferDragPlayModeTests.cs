using System.Collections;
using System.Collections.Generic;
using DeadManZone.Core.Shop;
using DeadManZone.Presentation.Shop;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class ShopOfferDragPlayModeTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp() => PlayModeTestHelpers.CleanupPersistentManagers();

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator SetPreviewVisible_TogglesPreviewRoot()
        {
            _root = new GameObject("OfferCard");
            var rect = _root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 180f);

            var previewGo = new GameObject("PreviewRoot");
            previewGo.transform.SetParent(_root.transform, false);
            previewGo.AddComponent<RectTransform>();

            var view = _root.AddComponent<ShopOfferView>();
            view.InitializeForTests(previewGo.GetComponent<RectTransform>());

            var offer = new ShopOffer
            {
                OfferId = "test",
                Lane = ShopLane.Offensive,
                PieceId = "conscript_rifleman",
                GoldPrice = 3
            };

            view.Bind(offer, false, 48f, 3f, 600f, 120f);
            yield return null;

            Assert.IsTrue(view.IsPreviewVisible);

            view.SetPreviewVisible(false);
            Assert.IsFalse(view.IsPreviewVisible);

            view.SetPreviewVisible(true);
            Assert.IsTrue(view.IsPreviewVisible);
            yield return null;
        }
    }
}
