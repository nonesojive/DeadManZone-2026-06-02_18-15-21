using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.Run;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class RunHudViewTests
    {
        [Test]
        public void Refresh_WiresResourceNumbersFromHierarchyWhenRefsMissing()
        {
            var topBar = new GameObject("TopBar", typeof(RectTransform));
            var panel = new GameObject("TopResourcePanel", typeof(RectTransform));
            panel.transform.SetParent(topBar.transform, false);

            var supplies = CreateNumberText(panel.transform, "SuppliesNumber");
            var manpower = CreateNumberText(panel.transform, "ManpowerNumber");
            var authority = CreateNumberText(panel.transform, "AuthorityNumber");
            var fightNumber = CreateNumberText(panel.transform, "FightNumber");

            var hud = topBar.AddComponent<RunHudView>();
            var state = new RunState
            {
                Supplies = 12,
                Manpower = 34,
                Authority = 56,
                FightIndex = 4
            };

            try
            {
                hud.Refresh(state);

                Assert.AreEqual("12", supplies.text);
                Assert.AreEqual("34", manpower.text);
                Assert.AreEqual("56", authority.text);
                Assert.AreEqual("4", fightNumber.text);
            }
            finally
            {
                Object.DestroyImmediate(topBar);
            }
        }

        [Test]
        public void RefreshPlayerStrength_WiresStrengthNumberFromTopInfoPanel()
        {
            var topInfoPanel = new GameObject("TopInfoPanel", typeof(RectTransform));
            var strengthNumber = CreateNumberText(topInfoPanel.transform, "StrengthNumber");
            var hud = topInfoPanel.AddComponent<RunHudView>();

            try
            {
                hud.RefreshPlayerStrength(new ArmyStrengthSnapshot
                {
                    BaseTotal = 900,
                    EffectiveTotal = 1240
                });

                Assert.AreEqual("1,240", strengthNumber.text);
            }
            finally
            {
                Object.DestroyImmediate(topInfoPanel);
            }
        }

        [Test]
        public void RefreshIncomePreview_WiresIncomeLabelsFromHierarchy()
        {
            var topBar = new GameObject("TopBar", typeof(RectTransform));
            var panel = new GameObject("TopResourcePanel", typeof(RectTransform));
            panel.transform.SetParent(topBar.transform, false);

            var suppliesIncome = CreateNumberText(panel.transform, "SuppliesIncome");
            var manpowerIncome = CreateNumberText(panel.transform, "ManpowerIncome");
            var authorityIncome = CreateNumberText(panel.transform, "AuthorityIncome");
            var hud = topBar.AddComponent<RunHudView>();

            try
            {
                hud.RefreshIncomePreview(new RoundIncomePreview(22, 14, 3, 18));

                Assert.AreEqual("+22", suppliesIncome.text);
                Assert.AreEqual("+14", manpowerIncome.text);
                Assert.AreEqual("3", authorityIncome.text);
            }
            finally
            {
                Object.DestroyImmediate(topBar);
            }
        }

        [Test]
        public void RefreshIncomePreview_WiresSalvageNumberFromHierarchy()
        {
            var topBar = new GameObject("TopBar", typeof(RectTransform));
            var panel = new GameObject("TopResourcePanel", typeof(RectTransform));
            panel.transform.SetParent(topBar.transform, false);

            var salvageNumber = CreateNumberText(panel.transform, "SalvageNumber");
            var hud = topBar.AddComponent<RunHudView>();

            try
            {
                hud.RefreshIncomePreview(new RoundIncomePreview(0, 0, 0, 23));

                Assert.AreEqual("23%", salvageNumber.text);
            }
            finally
            {
                Object.DestroyImmediate(topBar);
            }
        }

        [Test]
        public void RefreshMatchupFromBoards_UpdatesStrengthFromPlayerBoard()
        {
            var topInfoPanel = new GameObject("TopInfoPanel", typeof(RectTransform));
            var strengthNumber = CreateNumberText(topInfoPanel.transform, "StrengthNumber");
            var hud = topInfoPanel.AddComponent<RunHudView>();
            var playerBoard = TestBoards.StandardPlayer();

            try
            {
                hud.RefreshMatchupFromBoards(playerBoard, null);

                var expected = ArmyStrengthCalculator.Evaluate(playerBoard).EffectiveTotal;
                Assert.AreEqual(expected.ToString("N0"), strengthNumber.text);
            }
            finally
            {
                Object.DestroyImmediate(topInfoPanel);
            }
        }

        private static TextMeshProUGUI CreateNumberText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }
    }
}
