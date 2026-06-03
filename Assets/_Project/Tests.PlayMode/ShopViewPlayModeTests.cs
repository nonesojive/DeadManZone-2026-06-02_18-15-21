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

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator Render_CreatesOfferCardsAndTooltipText()
        {
            _root = new GameObject("ShopRoot");
            var view = _root.AddComponent<ShopView>();

            var laneRoot = new GameObject("GeneralRoot").transform;
            laneRoot.SetParent(_root.transform, false);
            var engineerRoot = new GameObject("EngineerRoot").transform;
            engineerRoot.SetParent(_root.transform, false);
            var reqRoot = new GameObject("ReqRoot").transform;
            reqRoot.SetParent(_root.transform, false);

            var tooltipGo = new GameObject("Tooltip");
            tooltipGo.transform.SetParent(_root.transform, false);
            var tooltip = tooltipGo.AddComponent<TextMeshProUGUI>();

            var prefab = new GameObject("OfferPrefab");
            prefab.SetActive(false);
            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<ShopOfferView>();

            view.InitializeForTests(laneRoot, engineerRoot, reqRoot, prefab, tooltip);

            var state = new ShopState
            {
                Offers = new List<ShopOffer>
                {
                    new() { OfferId = "a", Lane = ShopLane.Offensive, PieceId = "rifle_squad", GoldPrice = 3 },
                    new() { OfferId = "b", Lane = ShopLane.Defensive, PieceId = "command_bunker", GoldPrice = 7 },
                    new() { OfferId = "c", Lane = ShopLane.Specialty, PieceId = "mortar_crew", RequisitionPrice = 2 }
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

            Assert.AreEqual(1, laneRoot.childCount);
            Assert.AreEqual(1, engineerRoot.childCount);
            Assert.AreEqual(1, reqRoot.childCount);
            StringAssert.Contains("10% gold discount", tooltip.text);
            StringAssert.Contains("extra general slot", tooltip.text.ToLowerInvariant());
            StringAssert.Contains("Armored", tooltip.text);
        }
    }
}
