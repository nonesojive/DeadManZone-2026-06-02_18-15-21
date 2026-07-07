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
            EnsureOpaqueBacking(activeCard.transform as RectTransform);
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

        /// <summary>Docks the card to whichever screen edge is opposite the pointer,
        /// keeping the center column free, clamped in true screen space so it never spills
        /// off any edge. Clamping against the parent rect (which is not full-screen) let the
        /// top of the card run off the top of the screen for top-row offers.</summary>
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

            // Card size is in canvas units; convert to screen pixels so the clamp is against
            // the real screen, independent of how big the panel's parent rect is.
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            if (scale <= 0f)
                scale = 1f;
            float halfW = panelRoot.rect.width * 0.5f * scale;
            float halfH = panelRoot.rect.height * 0.5f * scale;
            const float margin = 20f;

            float targetX = pointerScreenPosition.x < Screen.width * 0.5f
                ? Screen.width - halfW - margin   // pointer on the left → card on the right
                : halfW + margin;                 // pointer on the right → card on the left
            float targetY = Mathf.Clamp(
                pointerScreenPosition.y,
                halfH + margin,
                Screen.height - halfH - margin);

            // ScreenPointToLocalPointInRectangle returns a point relative to the parent's
            // pivot, so the child's anchor must sit at that pivot for anchoredPosition to
            // line up — otherwise a non-centered parent pivot shifts the card off one edge.
            panelRoot.anchorMin = panelRoot.anchorMax = parent.pivot;
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, new Vector2(targetX, targetY), cam, out var local))
                panelRoot.anchoredPosition = local;
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

        private const string BackingName = "OpaqueBacking";
        private static readonly Color BackingColor = new(0.09f, 0.085f, 0.08f, 1f);

        /// <summary>The authored card frame has a transparent interior, so the shop/board
        /// showed through it. Insert a solid dark backing as the first (rearmost) child so
        /// the card reads as an opaque panel wherever it hovers.</summary>
        private static void EnsureOpaqueBacking(RectTransform card)
        {
            if (card == null)
                return;

            var existing = card.Find(BackingName) as RectTransform;
            if (existing == null)
            {
                var go = new GameObject(BackingName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                existing = (RectTransform)go.transform;
                existing.SetParent(card, false);
                var image = go.GetComponent<Image>();
                image.color = BackingColor;
                image.raycastTarget = false;
                image.maskable = false;
            }

            existing.anchorMin = Vector2.zero;
            existing.anchorMax = Vector2.one;
            existing.offsetMin = Vector2.zero;
            existing.offsetMax = Vector2.zero;
            existing.SetAsFirstSibling(); // behind all card content
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
