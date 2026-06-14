using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
  /// <summary>Wires center-column HUD: unit card panel, messages, buff strip.</summary>
  public sealed class BuildScreenHudController : MonoBehaviour
  {
    [SerializeField] private Transform buildPanel;
    [SerializeField] private BoardView boardView;
    [SerializeField] private UnitCardPanelView unitCardPanel;
    [SerializeField] private BuildMessagesView messagesView;
    [SerializeField] private BuffIconStripView buffIconStrip;

    private void OnEnable()
    {
      ResolveReferences();
      if (RunManager.Instance != null)
        RunManager.Instance.RunStateChanged += OnRunStateChanged;
      RefreshBuffStrip();
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
      BuffIconStripView buffStrip = null)
    {
      buildPanel = panel;
      boardView = board;
      unitCardPanel = unitPanel;
      messagesView = messages;
      buffIconStrip = buffStrip;
      ResolveReferences();
      WireUnitCardPanel();
      RefreshBuffStrip();
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

      if (buffIconStrip == null)
        buffIconStrip = buildPanel.GetComponentInChildren<BuffIconStripView>(true);

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

    private void OnRunStateChanged(Core.Run.RunState _) => RefreshBuffStrip();

    private void RefreshBuffStrip()
    {
      if (buffIconStrip == null || boardView == null)
        return;

      buffIconStrip.Refresh(boardView.GetBoardState(), messagesView);
    }

    public BuildMessagesView Messages => messagesView;
  }
}
