using DeadManZone.Core.Run;
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
            var morale = CreateNumberText(panel.transform, "MoralNumber");
            var fightNumber = CreateNumberText(panel.transform, "FightNumber");

            var hud = topBar.AddComponent<RunHudView>();
            var state = new RunState
            {
                Supplies = 12,
                Manpower = 34,
                Authority = 56,
                Morale = 78,
                FightIndex = 4
            };

            try
            {
                hud.Refresh(state);

                Assert.AreEqual("12", supplies.text);
                Assert.AreEqual("34", manpower.text);
                Assert.AreEqual("56", authority.text);
                Assert.AreEqual("78", morale.text);
                Assert.AreEqual("4", fightNumber.text);
            }
            finally
            {
                Object.DestroyImmediate(topBar);
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
