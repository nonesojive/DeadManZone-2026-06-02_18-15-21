using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Fixed center-column detail panel; HQ pieces use building layout, combat pieces use unit layout.</summary>
    public sealed class UnitCardPanelView : MonoBehaviour
    {
        public const string UnitDetailCardName = "UnitDetailCard";
        public const string BuildingCardName = "BuildingPrefab";

        [SerializeField] private RectTransform panelRoot;
        [FormerlySerializedAs("cardView")]
        [SerializeField] private PieceCardView unitCardView;
        [SerializeField] private PieceCardView buildingCardView;

        private void Awake()
        {
            // Runtime-provisioned panels have no authored panelRoot; fall back to our own
            // rect so show/hide/positioning still work.
            if (panelRoot == null)
                panelRoot = transform as RectTransform;
        }

        public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

        /// <summary>Last bound card (unit or building), if any.</summary>
        public PieceCardView CardView { get; private set; }

        public static bool UsesBuildingCard(PieceDefinition definition) =>
            definition != null && BoardPlacementRules.ResolveTargetBoard(definition) == BoardKind.Hq;

        /// <summary>Wires scene/prefab instances. Never writes to authored card prefab assets.</summary>
        public void EnsureCardView()
        {
            var host = PanelHost;
            LegacyUnitCardCleanup.RemoveLegacyChildren(host);
            ResolveCardViews(host);
            EnsureBuildingCardInstance(host);
            if (unitCardView != null)
                CenterCardInPanel(unitCardView.transform as RectTransform);
            if (buildingCardView != null)
                CenterCardInPanel(buildingCardView.transform as RectTransform);
            SuppressPanelBackgroundIfCardPresent();
        }

        public void Show(PieceDefinition definition, PieceCardBuildContext context = null)
        {
            if (definition == null)
                return;

            var host = PanelHost;
            LegacyUnitCardCleanup.RemoveLegacyChildren(host);
            ResolveCardViews(host);
            EnsureBuildingCardInstance(host);

            var activeCard = SelectActiveCard(definition);
            if (activeCard == null)
            {
                Debug.LogWarning(
                    UsesBuildingCard(definition)
                        ? "UnitCardPanelView has no BuildingPrefab instance. Place or link one under UnitCardPanel in the Run scene."
                        : "UnitCardPanelView has no UnitDetailCard instance. Place or link one under UnitCardPanel in the Run scene.",
                    this);
                return;
            }

            DeactivateInactiveCard(definition, activeCard);

            var model = PieceCardViewModelBuilder.Build(definition, context);
            string overflowTooltip = PieceCardOverflowTooltip.Build(definition, model);
            activeCard.Bind(model, overflowTooltip);
            SuppressPanelBackgroundIfCardPresent();
            activeCard.Show();
            CardView = activeCard;

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            unitCardView?.Hide();
            buildingCardView?.Hide();
            CardView = null;

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(false);
        }

        /// <summary>Docks the card to whichever screen edge is opposite the pointer, keeping
        /// the center column free. Rather than assume the card's size (which broke when the
        /// panel/card was resized in the editor), it docks then measures the card's real
        /// rendered corners and nudges any overflow back on-screen — robust to any size.</summary>
        public void PositionOppositePointer(Vector2 pointerScreenPosition)
        {
            if (panelRoot == null)
                return;

            if (panelRoot.parent is not RectTransform parent)
                return;

            EnsureRendersOnTop();

            var canvas = panelRoot.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

            // Anchor to the parent's pivot so anchoredPosition maps cleanly to screen points.
            panelRoot.anchorMin = panelRoot.anchorMax = parent.pivot;
            panelRoot.pivot = new Vector2(0.5f, 0.5f);

            // Tentative: dock to the far edge opposite the pointer, level with it.
            bool pointerLeft = pointerScreenPosition.x < Screen.width * 0.5f;
            var tentative = new Vector2(pointerLeft ? Screen.width : 0f, pointerScreenPosition.y);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, tentative, cam, out var local))
                panelRoot.anchoredPosition = local;

            // Correct precisely against the actual on-screen card rect.
            Canvas.ForceUpdateCanvases();
            var target = CardView != null ? (RectTransform)CardView.transform : panelRoot;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            const float margin = 12f;
            float dx = 0f, dy = 0f;
            if (bottomLeft.x < margin)
                dx = margin - bottomLeft.x;
            else if (topRight.x > Screen.width - margin)
                dx = (Screen.width - margin) - topRight.x;
            if (bottomLeft.y < margin)
                dy = margin - bottomLeft.y;
            else if (topRight.y > Screen.height - margin)
                dy = (Screen.height - margin) - topRight.y;

            if (Mathf.Abs(dx) > 0.5f || Mathf.Abs(dy) > 0.5f)
                panelRoot.anchoredPosition += new Vector2(dx, dy) / scale;
        }

        /// <summary>Give the card its own overriding canvas so it always draws above the
        /// shop and board, wherever it sits in the hierarchy.</summary>
        private void EnsureRendersOnTop()
        {
            if (panelRoot == null)
                return;

            var canvas = panelRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = panelRoot.gameObject.AddComponent<Canvas>();
                panelRoot.gameObject.AddComponent<GraphicRaycaster>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
        }

        private Transform PanelHost => panelRoot != null ? panelRoot : transform;

        private PieceCardView SelectActiveCard(PieceDefinition definition) =>
            UsesBuildingCard(definition) ? buildingCardView : unitCardView;

        private void DeactivateInactiveCard(PieceDefinition definition, PieceCardView activeCard)
        {
            var inactive = UsesBuildingCard(definition) ? unitCardView : buildingCardView;
            if (inactive == null || inactive == activeCard)
                return;

            inactive.Hide();
            inactive.gameObject.SetActive(false);
            activeCard.gameObject.SetActive(true);
        }

        private void ResolveCardViews(Transform host)
        {
            if (unitCardView == null)
                unitCardView = FindNamedCardView(host, UnitDetailCardName);

            if (unitCardView == null)
            {
                foreach (var view in host.GetComponentsInChildren<PieceCardView>(true))
                {
                    if (view.gameObject.name == BuildingCardName)
                        continue;

                    unitCardView = view;
                    break;
                }
            }

            if (buildingCardView == null)
                buildingCardView = FindNamedCardView(host, BuildingCardName);
        }

        private void EnsureBuildingCardInstance(Transform host)
        {
            if (buildingCardView != null)
                return;

            var prefab = CardPrefabRuntimeLoader.LoadPrefab(CardPrefabPaths.BuildingPrefab);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"UnitCardPanelView could not load building card prefab at '{CardPrefabPaths.BuildingPrefab}'.",
                    this);
                return;
            }

            var cardGo = Instantiate(prefab, host);
            cardGo.name = BuildingCardName;
            CenterCardInPanel(cardGo.GetComponent<RectTransform>());
            buildingCardView = cardGo.GetComponent<PieceCardView>();
            cardGo.SetActive(false);
        }

        private static PieceCardView FindNamedCardView(Transform host, string childName)
        {
            var child = host.Find(childName);
            return child != null ? child.GetComponent<PieceCardView>() : null;
        }

        /// <summary>Preserves prefab size and child offsets; stretch-fill breaks center-anchored authored layouts.</summary>
        public static void CenterCardInPanel(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            // ponytail: both card prefabs use 450×700; stretch wiring zeroes sizeDelta.
            if (rect.sizeDelta.x < 1f || rect.sizeDelta.y < 1f)
                rect.sizeDelta = new Vector2(450f, 700f);
        }

        private void SuppressPanelBackgroundIfCardPresent()
        {
            if (unitCardView == null && buildingCardView == null)
                return;

            var host = PanelHost;
            var panelImage = host.GetComponent<Image>();
            if (panelImage != null)
                panelImage.enabled = false;
        }
    }
}
