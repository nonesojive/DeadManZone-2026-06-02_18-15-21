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
    public sealed class ShopViewPlayModeTests
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
        public IEnumerator Render_CreatesOfferCardsInUnifiedGrid()
        {
            _root = new GameObject("ShopRoot");
            var view = _root.AddComponent<ShopView>();

            var gridRoot = new GameObject("OffersGrid", typeof(RectTransform)).transform;
            gridRoot.SetParent(_root.transform, false);

            var tooltipGo = new GameObject("Tooltip");
            tooltipGo.transform.SetParent(_root.transform, false);
            var tooltip = tooltipGo.AddComponent<TextMeshProUGUI>();

            var prefab = new GameObject("OfferPrefab");
            prefab.SetActive(false);
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<ShopOfferView>();

            view.InitializeForTests(gridRoot, prefab, tooltip);

            var state = new ShopState
            {
                Offers = new List<ShopOffer>
                {
                    new() { OfferId = "a", Lane = ShopLane.Offensive, SlotIndex = 0, PieceId = "rifle_squad", GoldPrice = 3 },
                    new() { OfferId = "b", Lane = ShopLane.Defensive, SlotIndex = 3, PieceId = "command_bunker", GoldPrice = 7 },
                    new() { OfferId = "c", Lane = ShopLane.Specialty, SlotIndex = 6, PieceId = "mortar_crew", RequisitionPrice = 2 }
                },
                Modifiers = new ShopModifiers
                {
                    GoldDiscountPercent = 10,
                    ExtraGeneralSlots = 1,
                    EnemyTagPreview = true
                }
            };

            view.Render(state, "Armored");
            yield return null;

            Assert.AreEqual(ShopSlotLayoutResolver.VisibleOfferSlotCount, gridRoot.childCount);
            StringAssert.Contains("10% gold discount", tooltip.text);
            StringAssert.Contains("extra shop slot", tooltip.text.ToLowerInvariant());
            StringAssert.Contains("Armored", tooltip.text);
        }
    }
}
