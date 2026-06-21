using System.Reflection;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class ReservesPieceFootprintHitTests
    {
        [Test]
        public void OnPointerEnter_ShowsUnitCardPanel()
        {
            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

            var shapeGo = new GameObject("PieceShape", typeof(RectTransform), typeof(Image));
            shapeGo.transform.SetParent(canvasGo.transform, false);
            var footprintHit = shapeGo.AddComponent<ReservesPieceFootprintHit>();
            footprintHit.Configure(
                "reserve-1",
                TestPieces.RifleSquad(),
                new GridCoord(0, 0),
                PieceRotation.R0,
                controller,
                boardView: null);

            try
            {
                footprintHit.OnPointerEnter(new PointerEventData(eventSystemGo.GetComponent<EventSystem>()));
                Assert.IsTrue(panelGo.activeSelf, "Unit card panel should open when hovering a reserves piece.");
            }
            finally
            {
                Object.DestroyImmediate(eventSystemGo);
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void OnPointerExit_HidesUnitCardPanel()
        {
            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

            var shapeGo = new GameObject("PieceShape", typeof(RectTransform), typeof(Image));
            shapeGo.transform.SetParent(canvasGo.transform, false);
            var footprintHit = shapeGo.AddComponent<ReservesPieceFootprintHit>();
            footprintHit.Configure(
                "reserve-1",
                TestPieces.RifleSquad(),
                new GridCoord(0, 0),
                PieceRotation.R0,
                controller,
                boardView: null);

            var eventData = new PointerEventData(eventSystemGo.GetComponent<EventSystem>());

            try
            {
                footprintHit.OnPointerEnter(eventData);
                Assert.IsTrue(panelGo.activeSelf);

                footprintHit.OnPointerExit(eventData);
                Assert.IsFalse(panelGo.activeSelf, "Unit card panel should close when hover leaves a reserves piece.");
            }
            finally
            {
                Object.DestroyImmediate(eventSystemGo);
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
