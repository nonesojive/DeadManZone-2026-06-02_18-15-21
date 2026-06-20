using System.Reflection;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class UnitCardPanelViewTests
    {
        [Test]
        public void EnsureCardView_RemovesLegacyUnitCardChild_WhenPieceCardViewAlreadyPresent()
        {
            var panelGo = new GameObject("UnitCardPanel", typeof(RectTransform));
            var panelView = panelGo.AddComponent<UnitCardPanelView>();

            var legacyGo = new GameObject("UnitCard", typeof(RectTransform));
            legacyGo.transform.SetParent(panelGo.transform, false);

            var cardGo = new GameObject("UnitDetailCard", typeof(RectTransform));
            cardGo.transform.SetParent(panelGo.transform, false);
            var cardView = cardGo.AddComponent<PieceCardView>();
            WireMinimalCard(cardView, cardGo.transform);

            SetPrivateField(panelView, "panelRoot", panelGo.GetComponent<RectTransform>());
            SetPrivateField(panelView, "cardView", cardView);

            try
            {
                panelView.EnsureCardView();

                Assert.IsNull(panelGo.transform.Find("UnitCard"));
                Assert.AreEqual(cardView, panelGo.GetComponentInChildren<PieceCardView>(true));
                Assert.AreEqual(1, panelGo.GetComponentsInChildren<PieceCardView>(true).Length);
            }
            finally
            {
                Object.DestroyImmediate(panelGo);
            }
        }

        [Test]
        public void EnsureCardView_DoesNotAddSecondCard_WhenPieceCardViewChildExists()
        {
            var panelGo = new GameObject("UnitCardPanel", typeof(RectTransform));
            var panelView = panelGo.AddComponent<UnitCardPanelView>();

            var cardGo = new GameObject("UnitDetailCard", typeof(RectTransform));
            cardGo.transform.SetParent(panelGo.transform, false);
            var cardView = cardGo.AddComponent<PieceCardView>();
            WireMinimalCard(cardView, cardGo.transform);

            SetPrivateField(panelView, "panelRoot", panelGo.GetComponent<RectTransform>());

            try
            {
                panelView.EnsureCardView();

                Assert.AreEqual(1, panelGo.GetComponentsInChildren<PieceCardView>(true).Length);
                Assert.AreEqual(cardView, panelGo.GetComponentInChildren<PieceCardView>(true));
            }
            finally
            {
                Object.DestroyImmediate(panelGo);
            }
        }

        private static void WireMinimalCard(PieceCardView cardView, Transform host)
        {
            var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            name.transform.SetParent(host, false);
            var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            hp.transform.SetParent(host, false);
            cardView.InitializeForTests(name, hp);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
