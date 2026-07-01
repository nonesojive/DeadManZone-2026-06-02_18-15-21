using DeadManZone.Core.Board;
using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
  /// <summary>Wires center-column HUD: unit card panel, messages, critical mass drawer.</summary>
  public sealed class BuildScreenHudController : MonoBehaviour
  {
    [SerializeField] private Transform buildPanel;
    [SerializeField] private BoardView boardView;
    [SerializeField] private UnitCardPanelView unitCardPanel;
    [SerializeField] private BuildMessagesView messagesView;
    [SerializeField] private CriticalMassDrawerView criticalMassDrawer;

    private void OnEnable()
    {
      ResolveReferences();
      if (RunManager.Instance != null)
        RunManager.Instance.RunStateChanged += OnRunStateChanged;
      RefreshCriticalMassDrawer();
    }

    private void OnDisable()
    {
      if (RunManager.Instance != null)
        RunManager.Instance.RunStateChanged -= OnRunStateChanged;
    }

    public void Configure(
      Transform panel,
      BoardView board,
      UnitCardPanelView unitPanel = null,
      BuildMessagesView messages = null,
      CriticalMassDrawerView drawer = null)
    {
      buildPanel = panel;
      boardView = board;
      unitCardPanel = unitPanel;
      messagesView = messages;
      criticalMassDrawer = drawer;
      ResolveReferences();
      WireUnitCardPanel();
      RefreshCriticalMassDrawer();
    }

    private void ResolveReferences()
    {
      if (buildPanel == null)
        buildPanel = transform;

      if (boardView == null)
        boardView = FindFirstObjectByType<BoardView>();

      if (unitCardPanel == null)
        unitCardPanel = buildPanel.GetComponentInChildren<UnitCardPanelView>(true);

      if (messagesView == null)
        messagesView = buildPanel.GetComponentInChildren<BuildMessagesView>(true);

      if (criticalMassDrawer == null)
        criticalMassDrawer = CriticalMassDrawerBootstrap.Ensure(buildPanel);

      WireUnitCardPanel();
    }

    private void WireUnitCardPanel()
    {
      if (boardView == null)
        return;

      var hoverController = boardView.GetComponent<PieceHoverCardController>();
      if (hoverController == null)
        hoverController = boardView.GetComponentInChildren<PieceHoverCardController>(true);

      if (unitCardPanel != null)
        hoverController?.SetFixedUnitCardPanel(unitCardPanel);

      if (messagesView != null)
        hoverController?.SetMessagesView(messagesView);
    }

    private void OnRunStateChanged(Core.Run.RunState _) => RefreshCriticalMassDrawer();

    public static void RequestRefresh()
    {
        foreach (var controller in FindObjectsByType<BuildScreenHudController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            controller.RefreshCriticalMassDrawer();
        RunHudIncomeRefresher.Refresh();
    }

    private void RefreshCriticalMassDrawer()
    {
      if (criticalMassDrawer == null)
        return;

      var boards = ResolveBuildBoards();
      if (boards == null)
        return;

      criticalMassDrawer.Refresh(boards);
    }

    private static BuildBoardSet ResolveBuildBoards()
    {
      if (RunManager.Instance != null && RunManager.Instance.HasActiveRun)
        return RunManager.Instance.Orchestrator.GetBuildBoards();

      var combatView = BoardView.FindCombatBoard();
      var hqView = BoardView.FindByBinding(BoardKind.Hq);
      if (combatView == null && hqView == null)
        return null;

      return new BuildBoardSet
      {
        Combat = combatView != null ? combatView.GetBoardState() : null,
        Hq = hqView != null ? hqView.GetBoardState() : null
      };
    }

    public BuildMessagesView Messages => messagesView;
  }
}
