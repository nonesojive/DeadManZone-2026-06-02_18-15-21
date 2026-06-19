using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Fixed center-column unit detail panel (hidden when idle).</summary>
    public sealed class UnitCardPanelView : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [FormerlySerializedAs("unitCard")]
        [SerializeField] private PieceCardView cardView;

        public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

        public void EnsureCardView()
        {
            if (cardView != null)
                return;

            cardView = GetComponentInChildren<PieceCardView>(true);
            if (cardView != null)
                return;

            var host = panelRoot != null ? panelRoot : transform;

            var legacy = host.GetComponentInChildren<PieceHoverCard>(true);
            if (legacy != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(legacy.gameObject);
                else
#endif
                    Destroy(legacy.gameObject);
            }

            var prefab = CardPrefabRuntimeLoader.LoadPrefab(CardPrefabPaths.UnitDetailCard);
            if (prefab == null)
            {
                Debug.LogWarning("UnitCardPanelView could not load UnitDetailCard prefab.", this);
                return;
            }

            var cardGo = Instantiate(prefab, host);
            cardGo.name = prefab.name;
            cardView = cardGo.GetComponent<PieceCardView>();
        }

        public void Show(PieceDefinition definition, PieceCardBuildContext context = null)
        {
            if (definition == null)
                return;

            if (cardView == null)
                EnsureCardView();

            if (cardView == null)
            {
                Debug.LogError("UnitCardPanelView is missing cardView reference.", this);
                return;
            }

            var model = PieceCardViewModelBuilder.Build(definition, context);
            string overflowTooltip = PieceCardOverflowTooltip.Build(definition, model);
            cardView.Bind(model, overflowTooltip);
            cardView.Show();

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            cardView?.Hide();
            if (panelRoot != null)
                panelRoot.gameObject.SetActive(false);
        }
    }
}
