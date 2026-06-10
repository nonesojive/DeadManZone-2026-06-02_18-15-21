using System.Collections;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Combat.Arena;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
  public sealed class CombatBoardPresenter : MonoBehaviour
  {
    [SerializeField] private BoardView boardView;
    [SerializeField] private CombatDirector combatDirector;
    [SerializeField] private TMP_Text phaseBannerText;
    [SerializeField] private CanvasGroup phaseBannerGroup;

    private readonly Dictionary<string, GridCoord> _instanceAnchors = new();
    private readonly CombatReplayVisuals _replayVisuals = new();
    private ContentRegistry _registry;
    private Coroutine _bannerRoutine;

    private void Awake()
    {
      if (combatDirector == null)
        combatDirector = GetComponent<CombatDirector>();

      var database = ContentDatabase.Load();
      _registry = ContentRegistryProvider.Build(database);
    }

    private void OnEnable()
    {
      if (combatDirector != null)
        combatDirector.EventReplayed += OnEventReplayed;

      if (RunManager.Instance != null)
        RunManager.Instance.CombatAdvanced += OnCombatAdvanced;

      HideBanner();
      boardView?.RefreshFromRunManager();
      InitializeReplayState();
    }

    private void OnDisable()
    {
      if (combatDirector != null)
        combatDirector.EventReplayed -= OnEventReplayed;

      if (RunManager.Instance != null)
        RunManager.Instance.CombatAdvanced -= OnCombatAdvanced;
    }

    private void OnCombatAdvanced(CombatAdvanceResult result)
    {
      if (CombatPresentationMode.ArenaActive)
        return;

      RestoreReplayStateBeforeSegment(result.CompletedPhase);
      _replayVisuals.SyncBoardView(boardView);
      ShowPhaseBanner(result.CompletedPhase);
    }

    private void InitializeReplayState()
    {
      _instanceAnchors.Clear();
      if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun || _registry == null)
        return;

      var state = RunManager.Instance.State;
      if (state.Phase != RunPhase.Combat || state.Combat?.EnemyBoard == null)
        return;

      var excludePhase = state.Combat.AwaitingCommand ? state.Combat.CompletedPhase : (CombatPhase?)null;
      RestoreReplayStateBeforeSegment(excludePhase);
    }

    private void RestoreReplayStateBeforeSegment(CombatPhase? excludePhase)
    {
      if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun || _registry == null)
        return;

      var state = RunManager.Instance.State;
      if (state.Phase != RunPhase.Combat || state.Combat?.EnemyBoard == null)
        return;

      var playerBoard = RunManager.Instance.Orchestrator.GetPlayerBoard();
      var enemyBoard = BoardSnapshotMapper.ToBoard(state.Combat.EnemyBoard, _registry);
      var battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
      var events = ConvertSavedEvents(state.Combat.EventLog);

      _replayVisuals.RestoreFromBattlefieldAndEvents(battlefield, events, excludePhase);
      _instanceAnchors.Clear();
      foreach (var pair in _replayVisuals.Anchors)
        _instanceAnchors[pair.Key] = pair.Value;
    }

    private static IEnumerable<CombatEvent> ConvertSavedEvents(IReadOnlyList<CombatEventRecord> records)
    {
      if (records == null)
        yield break;

      foreach (var record in records)
      {
        yield return new CombatEvent
        {
          Phase = record.Phase,
          Tick = record.Tick,
          ActorId = record.ActorId,
          ActionType = record.ActionType,
          TargetId = record.TargetId,
          Value = record.Value
        };
      }
    }

    private void OnEventReplayed(CombatEvent combatEvent)
    {
      if (CombatPresentationMode.ArenaActive)
        return;

      if (combatEvent == null)
        return;

      _replayVisuals.ApplyEvent(combatEvent, boardView);
      foreach (var pair in _replayVisuals.Anchors)
        _instanceAnchors[pair.Key] = pair.Value;

      switch (combatEvent.ActionType)
      {
        case "move":
          FlashTile(combatEvent.ActorId, UiThemeProvider.Current.tileHoverColor);
          break;
        case "damage":
        case "gas_damage":
          FlashTile(combatEvent.TargetId, UiThemeProvider.Current.dangerColor);
          ShowFloatingText(combatEvent.TargetId, $"-{combatEvent.Value}");
          break;
        case "destroyed":
          FlashTile(combatEvent.ActorId, UiThemeProvider.Current.dangerColor);
          break;
        case "stance_change":
          FlashTile(combatEvent.ActorId, UiThemeProvider.Current.accentColor);
          break;
      }
    }

    private void FlashTile(string instanceId, Color flashColor)
    {
      if (!TryGetTile(instanceId, out var tile))
        return;

      tile.PulseHighlight(flashColor);
    }

    private void ShowFloatingText(string instanceId, string text)
    {
      if (!TryGetTile(instanceId, out var tile))
        return;

      var theme = UiThemeProvider.Current;
      var go = new GameObject("FloatText", typeof(RectTransform));
      go.transform.SetParent(tile.transform, false);
      var rect = go.GetComponent<RectTransform>();
      rect.anchorMin = new Vector2(0.5f, 1f);
      rect.anchorMax = new Vector2(0.5f, 1f);
      rect.pivot = new Vector2(0.5f, 0f);
      rect.anchoredPosition = Vector2.zero;
      rect.sizeDelta = new Vector2(80f, 28f);

      var tmp = go.AddComponent<TextMeshProUGUI>();
      tmp.text = text;
      tmp.fontSize = 18;
      tmp.fontStyle = FontStyles.Bold;
      tmp.alignment = TextAlignmentOptions.Center;
      tmp.color = theme.dangerColor;
      tmp.raycastTarget = false;

      Destroy(go, 0.9f);
    }

    private bool TryGetTile(string instanceId, out BoardTileView tile)
    {
      tile = null;
      if (boardView == null || string.IsNullOrEmpty(instanceId))
        return false;

      if (!_instanceAnchors.TryGetValue(instanceId, out var anchor))
        return false;

      tile = boardView.GetTile(anchor);
      return tile != null;
    }

    private void ShowPhaseBanner(CombatPhase phase)
    {
      if (phaseBannerText == null)
        return;

      phaseBannerText.text = phase switch
      {
        CombatPhase.Deployment => "Deployment",
        CombatPhase.Grind => "The Grind",
        CombatPhase.FinalPush => "Final Push",
        _ => phase.ToString()
      };

      if (_bannerRoutine != null)
        StopCoroutine(_bannerRoutine);

      _bannerRoutine = StartCoroutine(BannerRoutine());
    }

    private IEnumerator BannerRoutine()
    {
      if (phaseBannerGroup != null)
      {
        phaseBannerGroup.gameObject.SetActive(true);
        phaseBannerGroup.alpha = 1f;
        yield return new WaitForSeconds(1.2f);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
          phaseBannerGroup.alpha = 1f - t / 0.4f;
          yield return null;
        }

        phaseBannerGroup.alpha = 0f;
        phaseBannerGroup.gameObject.SetActive(false);
      }

      _bannerRoutine = null;
    }

    private void HideBanner()
    {
      if (phaseBannerGroup != null)
      {
        phaseBannerGroup.alpha = 0f;
        phaseBannerGroup.gameObject.SetActive(false);
      }
    }
  }
}
