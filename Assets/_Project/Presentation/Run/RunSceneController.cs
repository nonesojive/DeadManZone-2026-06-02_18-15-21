using System.Collections;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Shop;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    public sealed class RunSceneController : MonoBehaviour
    {
        [Header("Panels")]
        [FormerlySerializedAs("buildPanel")]
        [SerializeField] private GameObject shopScene;
        [SerializeField] private GameObject combatPanel;
        [FormerlySerializedAs("buildPanelCanvasGroup")]
        [SerializeField] private CanvasGroup shopSceneCanvasGroup;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private GameObject shopArea;
        [SerializeField] private GameObject bottomBar;
        [SerializeField] private BuildRowLayoutFitter mainRowLayout;

        private GameObject _topBar;
        private GameObject _mainRow;
        private GameObject _runHudPanel;

        [Header("Views")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardView hqBoardView;
        [SerializeField] private ShopView shopView;
        [SerializeField] private ReservesView reservesView;
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private TacticPausePanel tacticPausePanel;
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
            if (shopScene != null)
                RunUiAuthoringLock.EnsureOn(shopScene.transform);
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
                ShopUiBootstrap.EnsureOnShopArea(shopArea.transform, boardView, shopView?.ModifiersTooltip);
            }

            if (shopScene != null)
            {
                RunBuildUiBootstrap.EnsureOnBuildPanel(shopScene.transform, boardView, mainRowLayout);
                EnsureBuildScreenHudController();
                EnsureCenterColumnLayout();
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

        public void RefreshCombatPresentation()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            var state = RunManager.Instance.State;
            bool showBattlefield = state.Phase == RunPhase.Combat || state.Phase == RunPhase.Aftermath;
            SetCombatPresentationLayout(showBattlefield);
        }

        private void RefreshAll()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
            {
                runHudView?.Refresh(null);
                runEndOverlay?.Hide();
                pauseMenuView?.Hide();
                if (shopScene != null)
                    shopScene.SetActive(true);
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

            if (shopScene != null)
            {
                bool hideForArena = (inCombat || aftermath) && CombatArenaSession.IsActive;
                shopScene.SetActive(!hideForArena);
            }

            if (combatPanel != null)
                combatPanel.SetActive(inCombat || aftermath || runEnded);

            bool showBattlefield = inCombat || aftermath;
            SetCombatPresentationLayout(showBattlefield);

            if (inBuild && RunManager.Instance?.Orchestrator != null)
            {
                bool canStart = RunManager.Instance.Orchestrator.CanStartBattle(out string failureReason);
                if (beginFightButton != null)
                    beginFightButton.interactable = canStart;
                if (emergencyDraftButton != null)
                    emergencyDraftButton.gameObject.SetActive(false);
                runHudView?.Refresh(state, failureReason);
                if (boardView != null)
                {
                    try
                    {
                        var enemyBoard = RunManager.Instance.Orchestrator.GetUpcomingEnemyBoard();
                        runHudView?.RefreshMatchupFromBoards(boardView.GetBoardState(), enemyBoard);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Failed to build upcoming enemy preview: {ex.Message}");
                        runHudView?.RefreshMatchup(null);
                    }
                }
            }
            else
            {
                runHudView?.Refresh(state);
            }

            if (runEnded)
            {
                tacticPausePanel?.Hide();
                pauseMenuView?.Hide();
                runEndOverlay?.Show(state.Phase, state);
                return;
            }

            runEndOverlay?.Hide();

            if (inBuild)
            {
                boardView?.RefreshFromRunManager();
                hqBoardView?.RefreshFromRunManager();
                RefreshBuildUiLayout();
                if (mainRowLayout != null)
                    _buildBoardAnchorMax = mainRowLayout.BoardAnchorMax;
                boardView?.SyncLayoutFromBoard();
                hqBoardView?.SyncLayoutFromBoard();
                reservesView?.Refresh();
                shopView?.RefreshFromRunManager();
                tacticPausePanel?.Hide();
            }
            else if (inCombat || aftermath)
            {
                tacticPausePanel?.Hide();
            }
        }

        private void EnsureLayoutReferences()
        {
            if (shopScene == null)
                return;

            if (boardArea == null)
            {
                var board = shopScene.transform.Find("MainRow/BoardArea");
                if (board != null)
                    boardArea = board.GetComponent<RectTransform>();
            }

            if (shopArea == null)
            {
                var shop = shopScene.transform.Find("MainRow/ShopArea");
                if (shop != null)
                    shopArea = shop.gameObject;
            }

            if (bottomBar == null)
            {
                var bottom = shopScene.transform.Find("BottomBar");
                if (bottom != null)
                    bottomBar = bottom.gameObject;
            }

            if (_topBar == null)
            {
                var top = shopScene.transform.Find("TopBar");
                if (top != null)
                    _topBar = top.gameObject;
            }

            if (_mainRow == null)
            {
                var mainRow = shopScene.transform.Find("MainRow");
                if (mainRow != null)
                    _mainRow = mainRow.gameObject;
            }

            if (_runHudPanel == null)
            {
                var hudPanel = shopScene.transform.Find(RunHudPanelBuilder.PanelName);
                if (hudPanel == null)
                    hudPanel = shopScene.transform.Find("TopBar/" + RunHudPanelBuilder.PanelName);

                _runHudPanel = hudPanel != null ? hudPanel.gameObject : null;
            }

            if (boardView == null)
            {
                var combat = shopScene.transform.Find("MainRow/BoardArea/CombatBoardSection/CombatBoard");
                if (combat != null)
                    boardView = combat.GetComponent<BoardView>();
            }

            if (hqBoardView == null)
            {
                var hq = shopScene.transform.Find("MainRow/BoardArea/HqBoardSection/HqBoard");
                if (hq != null)
                    hqBoardView = hq.GetComponent<BoardView>();
            }

            if (reservesView == null)
            {
                var reserves = shopScene.transform.Find("MainRow/BoardArea/ReservesSection/ReservesRegion");
                if (reserves != null)
                    reservesView = reserves.GetComponent<ReservesView>();
            }
        }

        internal Transform ShopSceneTransform => shopScene != null ? shopScene.transform : null;

        internal Transform BuildPanelTransform => ShopSceneTransform;

        private void EnsureMainRowLayoutFitter()
        {
            if (shopScene == null || boardArea == null || shopArea == null || boardView == null)
                return;

            if (mainRowLayout == null)
            {
                var mainRow = shopScene.transform.Find("MainRow");
                if (mainRow == null)
                    return;

                mainRowLayout = mainRow.GetComponent<BuildRowLayoutFitter>();
                if (mainRowLayout == null)
                    mainRowLayout = mainRow.gameObject.AddComponent<BuildRowLayoutFitter>();
            }

            var shopRect = shopArea.GetComponent<RectTransform>();
            var centerRect = shopScene.transform.Find("MainRow/CenterColumn") as RectTransform;
            if (shopRect != null)
                mainRowLayout.Configure(boardArea, centerRect, shopRect, boardView);
        }

        private void EnsureBuildScreenHudController()
        {
            if (shopScene == null)
                return;

            var controller = shopScene.GetComponent<BuildScreenHudController>();
            if (controller == null)
                controller = shopScene.gameObject.AddComponent<BuildScreenHudController>();
            controller.Configure(
                shopScene.transform,
                boardView,
                shopScene.GetComponentInChildren<UnitCardPanelView>(true),
                shopScene.GetComponentInChildren<BuildMessagesView>(true),
                shopScene.GetComponentInChildren<BuffIconStripView>(true));
        }

        private void EnsureCenterColumnLayout()
        {
            if (shopScene == null || mainRowLayout == null)
                return;

            var messages = shopScene.GetComponentInChildren<BuildMessagesView>(true);
            var buffStrip = shopScene.transform.Find("BottomBar/BuffStripRegion") as RectTransform;
            if (buffStrip == null)
                buffStrip = shopScene.GetComponentInChildren<BuffIconStripView>(true)?.GetComponent<RectTransform>();

            CenterColumnLayoutFitter.EnsureOnBuildPanel(
                shopScene.transform,
                messages != null ? messages.GetComponent<RectTransform>() : null,
                buffStrip,
                mainRowLayout);
        }

        private void CaptureBuildLayout()
        {
            if (_buildLayoutCaptured || boardArea == null)
                return;

            if (mainRowLayout != null)
            {
                if (shopScene != null && RunUiAuthoringLock.ShouldSkipVisualMigration(shopScene.transform))
                    mainRowLayout.CacheAuthoringMetricsFromScene();
                else
                    mainRowLayout.ApplyLayout();
            }

            _buildBoardAnchorMax = mainRowLayout != null
                ? mainRowLayout.BoardAnchorMax
                : boardArea.anchorMax;
            _buildLayoutCaptured = true;
        }

        private void SetCombatPresentationLayout(bool combatActive)
        {
            EnsureLayoutReferences();

            if (shopArea != null)
                shopArea.SetActive(!combatActive);

            if (bottomBar != null)
                bottomBar.SetActive(!combatActive);

            if (_topBar != null)
                _topBar.SetActive(!combatActive);

            if (_mainRow != null)
                _mainRow.SetActive(!combatActive);

            if (_runHudPanel != null)
                _runHudPanel.SetActive(!combatActive);

            if (combatActive)
            {
                SetBuildPanelAlpha(0f);
                if (CombatArenaSession.IsActive && shopScene != null)
                    shopScene.SetActive(false);
            }
            else
            {
                SetBuildPanelAlpha(1f);
                if (shopScene != null)
                    shopScene.SetActive(true);
            }

            if (boardArea == null)
                return;

            if (combatActive)
            {
                boardArea.gameObject.SetActive(false);
                return;
            }

            boardArea.gameObject.SetActive(true);

            // Honor scene-authored board layout: only re-stretch the board area when the
            // authoring lock is absent. When locked, the designer's anchors/offsets are kept as-is.
            if (shopScene == null || !RunUiAuthoringLock.ShouldSkipVisualMigration(shopScene.transform))
            {
                boardArea.anchorMin = Vector2.zero;
                boardArea.anchorMax = _buildBoardAnchorMax;
                boardArea.offsetMin = Vector2.zero;
                boardArea.offsetMax = Vector2.zero;
            }

            RefreshBuildUiLayoutDeferred();
        }

        private void RefreshBuildUiLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (shopScene != null && RunUiAuthoringLock.ShouldSkipVisualMigration(shopScene.transform))
            {
                mainRowLayout?.CacheAuthoringMetricsFromScene();
                return;
            }

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
            hqBoardView?.SyncLayoutFromBoard();
            reservesView?.Refresh();
        }

        private void SetBuildPanelAlpha(float alpha)
        {
            if (shopSceneCanvasGroup == null)
                return;

            shopSceneCanvasGroup.alpha = alpha;
            shopSceneCanvasGroup.interactable = alpha > 0.9f;
            shopSceneCanvasGroup.blocksRaycasts = alpha > 0.9f;
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
