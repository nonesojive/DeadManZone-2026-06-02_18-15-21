using System.Collections;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    public sealed class RunSceneController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject buildPanel;
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private CanvasGroup buildPanelCanvasGroup;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private GameObject shopArea;
        [SerializeField] private GameObject bottomBar;
        [SerializeField] private BuildRowLayoutFitter mainRowLayout;

        [Header("Views")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private ShopView shopView;
        [SerializeField] private ReservesView reservesView;
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private TacticPausePanel tacticPausePanel;
        [SerializeField] private PhaseCommandPanel phaseCommandPanel;
        [SerializeField] private RunHudView runHudView;
        [SerializeField] private RunEndOverlayView runEndOverlay;
        [SerializeField] private PauseMenuView pauseMenuView;

        [Header("Hub")]
        [SerializeField] private Button beginFightButton;
        [SerializeField] private Button emergencyDraftButton;
        [SerializeField] private Button menuButton;

        private Vector2 _buildBoardAnchorMax = new(0.50f, 1f);
        private bool _buildLayoutCaptured;

        private void Awake()
        {
            EnsureLayoutReferences();
            EnsureMainRowLayoutFitter();
            CaptureBuildLayout();

            if (beginFightButton != null)
                beginFightButton.onClick.AddListener(OnBeginFight);
            if (emergencyDraftButton != null)
                emergencyDraftButton.onClick.AddListener(OnEmergencyDraft);
            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void Start()
        {
            RefreshBuildUiLayout();
            if (mainRowLayout != null)
                _buildBoardAnchorMax = mainRowLayout.BoardAnchorMax;

            if (shopArea != null)
            {
                ShopLaneLayoutFitter.EnsureOnShopArea(shopArea.transform);
                ShopUiBootstrap.EnsureOnShopArea(shopArea.transform, boardView, shopView?.ModifiersTooltip);
            }

            if (buildPanel != null)
            {
                RunBuildUiBootstrap.EnsureOnBuildPanel(buildPanel.transform, boardView, mainRowLayout);
                RefreshBuildUiLayout();
            }

            shopView?.RefreshFromRunManager();
        }

        private void OnEnable()
        {
            EnsureRunManager();
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
        }

        private void OnRunStateChanged(RunState state) => RefreshAll();

        private void RefreshAll()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
            {
                runHudView?.Refresh(null);
                runEndOverlay?.Hide();
                pauseMenuView?.Hide();
                if (buildPanel != null)
                    buildPanel.SetActive(true);
                if (combatPanel != null)
                    combatPanel.SetActive(false);
                SetBuildPanelAlpha(1f);
                return;
            }

            var state = RunManager.Instance.State;
            bool inBuild = state.Phase == RunPhase.Build;
            bool inCombat = state.Phase == RunPhase.Combat;
            bool aftermath = state.Phase == RunPhase.Aftermath;
            bool runEnded = state.Phase == RunPhase.Victory || state.Phase == RunPhase.Defeat;

            if (buildPanel != null)
                buildPanel.SetActive(true);

            if (combatPanel != null)
                combatPanel.SetActive(inCombat || aftermath || runEnded);

            bool showBattlefield = inCombat || aftermath;
            SetBuildPanelAlpha(1f);
            SetCombatPresentationLayout(showBattlefield);

            if (inBuild && RunManager.Instance?.Orchestrator != null)
            {
                bool canStart = RunManager.Instance.Orchestrator.CanStartBattle(out string failureReason);
                if (beginFightButton != null)
                    beginFightButton.interactable = canStart;
                if (emergencyDraftButton != null)
                {
                    emergencyDraftButton.gameObject.SetActive(!canStart && !state.EmergencyDraftUsed);
                    emergencyDraftButton.interactable = !canStart && !state.EmergencyDraftUsed;
                }
                runHudView?.Refresh(state, failureReason);
            }
            else
            {
                runHudView?.Refresh(state);
            }

            if (runEnded)
            {
                tacticPausePanel?.Hide();
                if (phaseCommandPanel != null)
                    phaseCommandPanel.Hide();
                pauseMenuView?.Hide();
                runEndOverlay?.Show(state.Phase, state);
                return;
            }

            runEndOverlay?.Hide();

            if (inBuild)
            {
                boardView?.RefreshFromRunManager();
                RefreshBuildUiLayout();
                if (mainRowLayout != null)
                    _buildBoardAnchorMax = mainRowLayout.BoardAnchorMax;
                boardView?.SyncLayoutFromBoard();
                reservesView?.Refresh();
                shopView?.RefreshFromRunManager();
                tacticPausePanel?.Hide();
                if (phaseCommandPanel != null)
                    phaseCommandPanel.Hide();
            }
            else if (inCombat || aftermath)
            {
                tacticPausePanel?.Hide();
                if (phaseCommandPanel != null)
                    phaseCommandPanel.Hide();
            }
        }

        private void EnsureLayoutReferences()
        {
            if (buildPanel == null)
                return;

            if (boardArea == null)
            {
                var board = buildPanel.transform.Find("MainRow/BoardArea");
                if (board != null)
                    boardArea = board.GetComponent<RectTransform>();
            }

            if (shopArea == null)
            {
                var shop = buildPanel.transform.Find("MainRow/ShopArea");
                if (shop != null)
                    shopArea = shop.gameObject;
            }

            if (bottomBar == null)
            {
                var bottom = buildPanel.transform.Find("BottomBar");
                if (bottom != null)
                    bottomBar = bottom.gameObject;
            }
        }

        private void EnsureMainRowLayoutFitter()
        {
            if (buildPanel == null || boardArea == null || shopArea == null || boardView == null)
                return;

            if (mainRowLayout == null)
            {
                var mainRow = buildPanel.transform.Find("MainRow");
                if (mainRow == null)
                    return;

                mainRowLayout = mainRow.GetComponent<BuildRowLayoutFitter>();
                if (mainRowLayout == null)
                    mainRowLayout = mainRow.gameObject.AddComponent<BuildRowLayoutFitter>();
            }

            var shopRect = shopArea.GetComponent<RectTransform>();
            if (shopRect != null)
                mainRowLayout.Configure(boardArea, shopRect, boardView);
        }

        private void CaptureBuildLayout()
        {
            if (_buildLayoutCaptured || boardArea == null)
                return;

            mainRowLayout?.ApplyLayout();
            _buildBoardAnchorMax = mainRowLayout != null
                ? mainRowLayout.BoardAnchorMax
                : boardArea.anchorMax;
            _buildLayoutCaptured = true;
        }

        private void SetCombatPresentationLayout(bool combatActive)
        {
            if (shopArea != null)
                shopArea.SetActive(!combatActive);

            if (bottomBar != null)
                bottomBar.SetActive(!combatActive);

            if (boardArea == null)
                return;

            if (combatActive)
            {
                if (mainRowLayout != null)
                    _buildBoardAnchorMax = mainRowLayout.BoardAnchorMax;

                boardArea.anchorMin = Vector2.zero;
                boardArea.anchorMax = Vector2.one;
                boardArea.offsetMin = Vector2.zero;
                boardArea.offsetMax = Vector2.zero;
                return;
            }

            boardArea.anchorMin = Vector2.zero;
            boardArea.anchorMax = _buildBoardAnchorMax;
            boardArea.offsetMin = Vector2.zero;
            boardArea.offsetMax = Vector2.zero;

            RefreshBuildUiLayoutDeferred();
        }

        private void RefreshBuildUiLayout()
        {
            Canvas.ForceUpdateCanvases();
            mainRowLayout?.InvalidateAndApply();
        }

        private void RefreshBuildUiLayoutDeferred()
        {
            StopCoroutine(nameof(DeferredBuildUiLayoutPass));
            StartCoroutine(DeferredBuildUiLayoutPass());
        }

        private IEnumerator DeferredBuildUiLayoutPass()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            RefreshBuildUiLayout();
            if (mainRowLayout != null)
                _buildBoardAnchorMax = mainRowLayout.BoardAnchorMax;
            boardView?.SyncLayoutFromBoard();
            reservesView?.Refresh();
        }

        private void SetBuildPanelAlpha(float alpha)
        {
            if (buildPanelCanvasGroup == null)
                return;

            buildPanelCanvasGroup.alpha = alpha;
            buildPanelCanvasGroup.interactable = alpha > 0.9f;
            buildPanelCanvasGroup.blocksRaycasts = alpha > 0.9f;
        }

        private void OnEmergencyDraft()
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.TryEmergencyDraft();
        }

        private void OnBeginFight()
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.BeginCombat();
        }

        private void OnMenuClicked()
        {
            if (pauseMenuView == null)
                return;

            if (pauseMenuView.IsOpen)
                pauseMenuView.Close();
            else
                pauseMenuView.Open();
        }

        private static void EnsureRunManager()
        {
            if (RunManager.Instance != null)
                return;

            var managerObject = new GameObject(nameof(RunManager));
            managerObject.AddComponent<RunManager>();
        }
    }
}
