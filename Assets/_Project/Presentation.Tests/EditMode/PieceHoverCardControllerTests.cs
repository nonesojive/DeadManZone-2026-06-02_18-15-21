using System.Reflection;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class PieceHoverCardControllerTests
    {
        [Test]
        public void Show_RoutesToUnitCardPanel_WithoutCreatingFloatingLayer()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var controllerGo = new GameObject("HoverController", typeof(RectTransform));
            controllerGo.transform.SetParent(canvasGo.transform, false);

            var panelGo = new GameObject("UnitCardPanel", typeof(RectTransform));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelView = panelGo.AddComponent<UnitCardPanelView>();

            var cardGo = new GameObject("UnitDetailCard", typeof(RectTransform));
            cardGo.transform.SetParent(panelGo.transform, false);
            var cardView = cardGo.AddComponent<PieceCardView>();
            var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            name.transform.SetParent(cardGo.transform, false);
            var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            hp.transform.SetParent(cardGo.transform, false);
            cardView.InitializeForTests(name, hp);

            SetPrivateField(panelView, "panelRoot", panelGo.GetComponent<RectTransform>());
            SetPrivateField(panelView, "cardView", cardView);

            var controller = controllerGo.AddComponent<PieceHoverCardController>();
            controller.SetFixedUnitCardPanel(panelView);

            try
            {
                controller.Show(TestPieces.RifleSquad(), Vector2.zero, new PieceCardBuildContext());

                Assert.IsTrue(panelGo.activeSelf);
                Assert.IsNull(canvasGo.transform.Find("PieceHoverCardLayer"));
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
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
