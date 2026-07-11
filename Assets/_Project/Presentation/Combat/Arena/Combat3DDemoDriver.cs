using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Play-mode harness for the Combat3D demo scene: builds two real 3-unit armies from
    /// ContentDatabase pieces, runs the fight to completion through the Core sim
    /// (<see cref="TickCombatRun"/> — full rules, deterministic seed, no house sim), then
    /// replays the event log segment-by-segment through <see cref="CombatDirector"/> onto
    /// the 3D actors. Command pauses are skipped (no commands submitted), the win banner
    /// fires from the real fight_end event. No RunManager/meta flow involved.
    /// </summary>
    public sealed class Combat3DDemoDriver : MonoBehaviour
    {
        [SerializeField] private CombatDirector director;
        [SerializeField] private CombatArenaPresenter presenter;
        [SerializeField] private CombatArenaSceneLoader arenaLoader;
        [SerializeField] private CombatArmyHealthHud armyHud;
        [SerializeField] private int combatSeed = 20260711;
        [Tooltip("ContentDatabase piece ids, one per unit. No grenade_thrower piece exists — ironclad_mortars wears that model.")]
        [SerializeField] private string[] playerRoster = { "conscript_rifleman", "bulwark_squad", "field_medic" };
        [SerializeField] private string[] enemyRoster = { "conscript_rifleman", "ironclad_mortars", "conscript_rifleman" };
        [Tooltip("Total length of the TACTICAL PAUSE beat shown between segments (fade in + hold + fade out).")]
        [SerializeField] private float pauseBeatSeconds = 1.5f;
        [Tooltip("Time dilation during the beat — the battlefield visibly holds its breath.")]
        [SerializeField] private float pauseBeatTimeScale = 0.35f;

        private const string PlayerIdPrefix = "p3d_unit";
        private const string EnemyIdPrefix = "e3d_unit";
        private const int MaxContinueCalls = 8;

        private bool _fightEndSeen;
        private CombatAdvanceResult _finalResult;
        private CanvasGroup _pauseBeatGroup;
        private bool _timeDilated;

        private IEnumerator Start()
        {
            director ??= GetComponent<CombatDirector>();
            presenter ??= GetComponent<CombatArenaPresenter>();
            arenaLoader ??= GetComponent<CombatArenaSceneLoader>();
            armyHud ??= GetComponent<CombatArmyHealthHud>();
            if (director == null || presenter == null)
            {
                Debug.LogError("[Combat3D] Demo driver needs CombatDirector + CombatArenaPresenter on the arena rig.", this);
                yield break;
            }

            var database = ContentDatabase.Load();
            var faction = database != null ? database.GetFaction(FactionIds.IronmarchUnion) : null;
            var playerPieces = ResolveRoster(database, playerRoster);
            var enemyPieces = ResolveRoster(database, enemyRoster);
            if (faction == null || playerPieces == null || enemyPieces == null)
            {
                Debug.LogError(
                    $"[Combat3D] ContentDatabase missing faction '{FactionIds.IronmarchUnion}' or a roster " +
                    "piece id (see warnings above). Run DeadManZone → Generate Demo Content (5 Factions) first.", this);
                yield break;
            }

            // The presenter gates replay events on the arena session being active; this scene
            // embeds the arena instead of additively loading the 2D arena scene.
            arenaLoader?.MarkEmbeddedArenaLoaded();

            // 1. Run the REAL fight through Core, offline, submitting no pause commands.
            var segments = new List<int>();
            var run = TickCombatRun.Start(
                BuildArmy(faction, playerPieces, PlayerIdPrefix),
                BuildArmy(faction, enemyPieces, EnemyIdPrefix),
                combatSeed);

            for (int i = 0; i < MaxContinueCalls; i++)
            {
                _finalResult = run.Continue(Array.Empty<PhaseCommand>());
                if (!segments.Contains(_finalResult.SegmentIndex))
                    segments.Add(_finalResult.SegmentIndex);
                if (_finalResult.Status == CombatAdvanceStatus.Completed)
                    break;
            }

            if (_finalResult == null || _finalResult.Status != CombatAdvanceStatus.Completed)
            {
                Debug.LogError("[Combat3D] Sim did not complete — check board setup/pacing config.", this);
                yield break;
            }

            Debug.Log(
                $"[Combat3D] Sim complete: {run.Log.Events.Count} events over {segments.Count} segment(s), " +
                $"playerWon={_finalResult.PlayerWon}, draw={_finalResult.IsDraw}.");

            // 2. Spawn 3D actors from an identical battlefield (same instance ids/anchors).
            var battlefield = BattlefieldState.FromBoards(
                BuildArmy(faction, playerPieces, PlayerIdPrefix),
                BuildArmy(faction, enemyPieces, EnemyIdPrefix));
            presenter.InitializeArena(battlefield);
            armyHud?.Initialize(battlefield); // army bars snap to 100% before playback

            director.EventReplayed += OnEventReplayed;

            // 3. Replay each segment at real pacing; command pauses become a TACTICAL
            // PAUSE beat (fight_end breaks first, so nothing shows after the final segment).
            foreach (int segment in segments)
            {
                director.PlayLog(run.Log, segment);
                yield return new WaitUntil(() => !director.IsPlaying);
                if (_fightEndSeen)
                    break;
                if (pauseBeatSeconds > 0f)
                    yield return TacticPauseBeat();
            }

            director.EventReplayed -= OnEventReplayed;

            yield return presenter.WaitForPendingDeathPresentations();
            ShowResultBanner(_finalResult);
        }

        private void OnDestroy()
        {
            if (director != null)
                director.EventReplayed -= OnEventReplayed;
            if (_timeDilated)
                Time.timeScale = 1f;
        }

        /// <summary>
        /// Watchable stand-in for the real tactic pause (the demo submits no commands, so
        /// the interactive TacticPausePanel is deliberately NOT wired): a grimdark
        /// "TACTICAL PAUSE" band fades in over a brief time dilation, holds, and fades out
        /// before the next segment plays. Presentation-only — the sim already ran to
        /// completion; this paces the replay and cannot affect determinism.
        /// </summary>
        private IEnumerator TacticPauseBeat()
        {
            Debug.Log("[Combat3D] Tactic pause beat (segment boundary).");
            EnsurePauseBeatUi();
            _pauseBeatGroup.gameObject.SetActive(true);

            Time.timeScale = Mathf.Clamp(pauseBeatTimeScale, 0.05f, 1f);
            _timeDilated = true;

            const float fadeIn = 0.25f, fadeOut = 0.35f;
            float hold = Mathf.Max(0.1f, pauseBeatSeconds - fadeIn - fadeOut);

            yield return FadePauseBeat(0f, 1f, fadeIn);
            yield return new WaitForSecondsRealtime(hold);
            yield return FadePauseBeat(1f, 0f, fadeOut);

            Time.timeScale = 1f;
            _timeDilated = false;
            _pauseBeatGroup.gameObject.SetActive(false);
        }

        private IEnumerator FadePauseBeat(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _pauseBeatGroup.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }

            _pauseBeatGroup.alpha = to;
        }

        /// <summary>Same canvas approach as the result banner, styled with the shared
        /// grimdark kit (dark band + bone lettering) so it reads as the pause panel's
        /// non-interactive cousin. Sits above the army HUD (400), under the banner (500).</summary>
        private void EnsurePauseBeatUi()
        {
            if (_pauseBeatGroup != null)
                return;

            var canvasGo = new GameObject("Combat3DTacticPauseBeat");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;
            _pauseBeatGroup = canvasGo.AddComponent<CanvasGroup>();
            _pauseBeatGroup.alpha = 0f;
            _pauseBeatGroup.blocksRaycasts = false;
            _pauseBeatGroup.interactable = false;

            CombatGrimdarkSkin.AddBand(canvasGo.transform, 0.66f, 0.78f, "PauseBand");

            var titleGo = new GameObject("PauseTitle");
            titleGo.transform.SetParent(canvasGo.transform, false);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "TACTICAL PAUSE";
            title.fontSize = 40f;
            title.alignment = TextAlignmentOptions.Center;
            CombatGrimdarkSkin.StyleTitle(title);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.685f);
            titleRect.anchorMax = new Vector2(1f, 0.78f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var subGo = new GameObject("PauseSubtitle");
            subGo.transform.SetParent(canvasGo.transform, false);
            var subtitle = subGo.AddComponent<TextMeshProUGUI>();
            subtitle.text = "ORDERS HOLD — COMBAT RESUMES";
            subtitle.fontSize = 18f;
            subtitle.characterSpacing = 4f;
            subtitle.alignment = TextAlignmentOptions.Center;
            CombatGrimdarkSkin.StyleBody(subtitle);
            var subRect = subtitle.rectTransform;
            subRect.anchorMin = new Vector2(0f, 0.66f);
            subRect.anchorMax = new Vector2(1f, 0.70f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            _pauseBeatGroup.gameObject.SetActive(false);
        }

        private void OnEventReplayed(CombatEvent combatEvent)
        {
            if (combatEvent == null || combatEvent.ActionType != "fight_end")
                return;

            _fightEndSeen = true;
            Debug.Log($"[Combat3D] fight_end replayed at segment {combatEvent.Segment}, tick {combatEvent.Tick}.");
        }

        /// <summary>Roster ids → piece definitions; null (with a warning per missing id) if any is unknown.</summary>
        private PieceDefinitionSO[] ResolveRoster(ContentDatabase database, string[] roster)
        {
            if (database?.Pieces == null || roster == null || roster.Length == 0)
                return null;

            var pieces = new PieceDefinitionSO[roster.Length];
            bool complete = true;
            for (int i = 0; i < roster.Length; i++)
            {
                pieces[i] = database.Pieces.FirstOrDefault(p => p != null && p.id == roster[i]);
                if (pieces[i] == null)
                {
                    Debug.LogWarning($"[Combat3D] Roster piece id '{roster[i]}' not found in ContentDatabase.", this);
                    complete = false;
                }
            }

            return complete ? pieces : null;
        }

        /// <summary>One army from the roster, built the same way the tests hand-build Core state.
        /// Rows advance by each piece's footprint height so multi-cell pieces never collide.</summary>
        private BoardState BuildArmy(FactionSO faction, PieceDefinitionSO[] pieces, string idPrefix)
        {
            var board = new BoardState(faction.CreateCombatBoardLayout());
            int row = 1; // column 4 = own front line-ish; rows fan out from the field's middle band
            for (int i = 0; i < pieces.Length; i++)
            {
                var core = pieces[i].ToCore();
                var anchor = new GridCoord(4, row);
                var result = board.TryPlace(core, anchor, $"{idPrefix}_{i + 1}");
                if (!result.Success)
                    Debug.LogError($"[Combat3D] Failed to place {pieces[i].id} at {anchor}: {result.Reason}", this);

                int footprintHeight = 1;
                foreach (var cell in core.Shape.GetCells(new GridCoord(0, 0), PieceRotation.R0))
                    footprintHeight = Mathf.Max(footprintHeight, cell.Y + 1);
                row += footprintHeight;
            }

            return board;
        }

        private void ShowResultBanner(CombatAdvanceResult result)
        {
            string text;
            Color color;
            if (result.IsDraw)
            {
                text = "DRAW — MUTUAL ANNIHILATION";
                color = new Color(0.85f, 0.82f, 0.72f);
            }
            else if (result.PlayerWon)
            {
                text = "VICTORY — PLAYER SIDE HOLDS THE FIELD";
                color = new Color(0.45f, 0.65f, 1f);
            }
            else
            {
                text = "DEFEAT — ENEMY SIDE TAKES THE FIELD";
                color = new Color(1f, 0.42f, 0.38f);
            }

            Debug.Log($"[Combat3D] {text}");

            var canvasGo = new GameObject("Combat3DResultBanner");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var textGo = new GameObject("BannerText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var banner = textGo.AddComponent<TextMeshProUGUI>();
            banner.text = text;
            banner.fontSize = 56f;
            banner.fontStyle = FontStyles.Bold;
            banner.alignment = TextAlignmentOptions.Center;
            banner.color = color;
            banner.outlineWidth = 0.2f;
            banner.outlineColor = new Color32(10, 10, 12, 255);

            var rect = banner.rectTransform;
            rect.anchorMin = new Vector2(0.05f, 0.4f);
            rect.anchorMax = new Vector2(0.95f, 0.6f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
