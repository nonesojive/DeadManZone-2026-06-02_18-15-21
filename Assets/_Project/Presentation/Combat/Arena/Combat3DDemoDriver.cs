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
    /// Play-mode harness for the Combat3D demo scene: builds two real armies from
    /// ContentDatabase pieces and runs the fight INTERLEAVED through the Core sim
    /// (<see cref="TickCombatRun"/> — full rules, deterministic seed): each Continue
    /// produces one segment, the segment replays through <see cref="CombatDirector"/>
    /// onto the 3D actors, and every command pause opens the real interactive tactics
    /// window (<see cref="CombatTacticOrdersWindow"/>) whose PhaseCommands feed the next
    /// Continue — the reference implementation for the main-build port. The sim is
    /// offline, so holding at a pause is pure presentation and cannot affect determinism.
    /// With <see cref="interactivePauses"/> off, pauses submit no commands and show the
    /// old TACTICAL PAUSE beat instead (headless-verification path). No RunManager/meta
    /// flow involved.
    /// </summary>
    public sealed class Combat3DDemoDriver : MonoBehaviour
    {
        [SerializeField] private CombatDirector director;
        [SerializeField] private CombatArenaPresenter presenter;
        [SerializeField] private CombatArenaSceneLoader arenaLoader;
        [SerializeField] private CombatArmyHealthHud armyHud;
        [SerializeField] private int combatSeed = 20260711;
        [Tooltip("ContentDatabase piece ids, one per unit. Optional ':Ability' suffix (testing aid) overrides the piece's grantedAbility via a runtime SO clone; assets untouched. Content ships real grants (armored_transport: ShieldAllies, ironclad_mortars: MortarShot), so the defaults are plain ids.")]
        // 2026-07-17 Wave 3 temp-art verification: vanquisher_doctrine_tank has no Meshy model
        // yet (Combat3DDemoSceneBootstrap.PrimitiveFallbackUnits) — swapped in for iron_guard
        // to confirm the grey-box primitive renders at the right footprint/height in Play mode.
        [SerializeField] private string[] playerRoster = { "conscript_rifles", "vanquisher_doctrine_tank", "field_mortar_team" };
        // Showcase default: the three new Meshy faces (marksman/field marshal/surgeon) on the
        // enemy side. assembly_trooper (Crimson Assembly, assault role, no model of its own)
        // swapped in for militia_squad to confirm humanoid-reuse (-> conscript_rifles folder)
        // and the faction ring-tint accent both render correctly.
        [SerializeField] private string[] enemyRoster = { "sharpshooter", "shock_sergeant", "assembly_trooper" };
        [Tooltip("Command pauses open the interactive tactics window. Off = the old auto path (no commands, pause beat between segments).")]
        [SerializeField] private bool interactivePauses = true;
        [Tooltip("Authority budget for the fight's command pauses (the real flow feeds this from the run's round pool).")]
        [SerializeField] private int startingAuthority = 8;
        [Tooltip("Non-interactive only: total length of the TACTICAL PAUSE beat shown between segments.")]
        [SerializeField] private float pauseBeatSeconds = 1.5f;
        [Tooltip("Non-interactive only: time dilation during the beat.")]
        [SerializeField] private float pauseBeatTimeScale = 0.35f;

        private const string PlayerIdPrefix = "p3d_unit";
        private const string EnemyIdPrefix = "e3d_unit";
        private const int MaxContinueCalls = 8;

        private readonly CommandProcessor _commandProcessor = new();
        private TickCombatRun _run;
        private FactionSO _faction;
        private CombatGridMapper _mapper;
        private CombatTacticOrdersWindow _ordersWindow;
        private bool _fightEndSeen;
        private CombatAdvanceResult _finalResult;
        private CanvasGroup _pauseBeatGroup;
        private bool _timeDilated;

        /// <summary>Verification seams (script-execute drives the window through these).</summary>
        public TickCombatRun ActiveRun => _run;
        public CombatTacticOrdersWindow OrdersWindow => _ordersWindow;
        public CombatGridMapper Mapper => _mapper;
        public bool IsAwaitingOrders => _ordersWindow != null && _ordersWindow.IsOpen;

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
            _faction = database != null ? database.GetFaction(FactionIds.IronmarchUnion) : null;
            var playerPieces = ResolveRoster(database, playerRoster);
            var enemyPieces = ResolveRoster(database, enemyRoster);
            if (_faction == null || playerPieces == null || enemyPieces == null)
            {
                Debug.LogError(
                    $"[Combat3D] ContentDatabase missing faction '{FactionIds.IronmarchUnion}' or a roster " +
                    "piece id (see warnings above). Run DeadManZone → Generate Demo Content (5 Factions) first.", this);
                yield break;
            }

            // The presenter gates replay events on the arena session being active; this scene
            // embeds the arena instead of additively loading the 2D arena scene.
            arenaLoader?.MarkEmbeddedArenaLoaded();

            // 1. The REAL fight through Core, offline. Mirrors RunOrchestrator.BeginCombat:
            // authority budget + default tactic applied before the opening pause.
            _run = TickCombatRun.Start(
                BuildArmy(_faction, playerPieces, PlayerIdPrefix),
                BuildArmy(_faction, enemyPieces, EnemyIdPrefix),
                combatSeed,
                startingAuthority);
            _run.SetPlayerTactic(ResolveDefaultTactic(_faction));

            // 2. Spawn 3D actors from an identical battlefield (same instance ids/anchors).
            var battlefield = BattlefieldState.FromBoards(
                BuildArmy(_faction, playerPieces, PlayerIdPrefix),
                BuildArmy(_faction, enemyPieces, EnemyIdPrefix));
            presenter.InitializeArena(battlefield);
            armyHud?.Initialize(battlefield); // army bars snap to 100% before playback

            var config = CombatArenaBootstrap.Instance != null ? CombatArenaBootstrap.Instance.Config : null;
            if (config != null)
                _mapper = new CombatGridMapper(battlefield.Layout, config.cellWidth, config.cellDepth);

            director.EventReplayed += OnEventReplayed;

            // 3. Interleaved loop: collect orders at every pause, Continue, replay the
            // produced segment at real pacing, repeat until the fight completes.
            var pending = new List<PhaseCommand>();
            for (int i = 0; i < MaxContinueCalls && !_fightEndSeen; i++)
            {
                if (_run.AwaitingCommand && interactivePauses)
                    yield return CollectOrders(pending);

                var result = _run.Continue(pending);
                pending.Clear();

                director.PlayLog(_run.Log, result.SegmentIndex);
                yield return new WaitUntil(() => !director.IsPlaying);

                if (result.Status == CombatAdvanceStatus.Completed)
                {
                    _finalResult = result;
                    break;
                }

                if (!interactivePauses && pauseBeatSeconds > 0f)
                    yield return TacticPauseBeat();
            }

            director.EventReplayed -= OnEventReplayed;

            if (_finalResult == null || _finalResult.Status != CombatAdvanceStatus.Completed)
            {
                Debug.LogError("[Combat3D] Sim did not complete — check board setup/pacing config.", this);
                yield break;
            }

            Debug.Log(
                $"[Combat3D] Fight complete: {_run.Log.Events.Count} events, " +
                $"playerWon={_finalResult.PlayerWon}, draw={_finalResult.IsDraw}, " +
                $"authorityLeft={_run.Requisition}.");

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

        /// <summary>Open the tactics window for the current pause and wait for RESUME.
        /// The window's commands land in <paramref name="pending"/> for the next Continue.</summary>
        private IEnumerator CollectOrders(List<PhaseCommand> pending)
        {
            EnsureOrdersWindow();

            bool submitted = false;
            _ordersWindow.Show(BuildPauseContext(), commands =>
            {
                pending.AddRange(commands);
                submitted = true;
            });

            Debug.Log($"[Combat3D] Tactics window open (pause {_run.CurrentPauseIndex}, authority {_run.Requisition}).");
            yield return new WaitUntil(() => submitted);
            Debug.Log($"[Combat3D] Orders submitted: {pending.Count} command(s).");
        }

        /// <summary>Snapshot of the run's pause state, shaped like the live flow's
        /// CombatPauseContext (RunOrchestrator.GetCombatPauseContext).</summary>
        private CombatTacticOrdersWindow.PauseContext BuildPauseContext()
        {
            var abilities = _commandProcessor
                .GetAvailableCommands(_run.PlayerBoard, _run.Requisition, _run.CurrentPauseIndex)
                .Where(c => c.Type == CommandType.UseAbility)
                .ToList();

            bool hasCommandPiece = _run.PlayerBoard.Pieces.Any(p =>
                p.Definition.CommandActions.HasFlag(CommandActionFlags.ChangeStance));

            return new CombatTacticOrdersWindow.PauseContext
            {
                CheckpointIndex = _run.CurrentPauseIndex,
                Authority = _run.Requisition,
                ActiveTactic = _run.PlayerTactic,
                HasCommandPiece = hasCommandPiece,
                StartingTactics = _faction != null ? _faction.startingTactics : null,
                AvailableAbilities = abilities,
                Trigger = _run.LastPauseTrigger,
                Mapper = _mapper,
                ArenaCamera = CombatArenaBootstrap.Instance != null ? CombatArenaBootstrap.Instance.ArenaCamera : null,
                // Mirrors CombatAbilityExecutor's honored-target rule (alive enemy anchor
                // at the cell). Demo-only read of the ForTests accessor; the main build
                // should surface enemy anchors through its pause context instead.
                IsValidAbilityTarget = cell => _run.EnemyCombatantsForTests.Any(e =>
                    e.IsAlive && e.AnchorPosition.Equals(cell))
            };
        }

        private void EnsureOrdersWindow()
        {
            if (_ordersWindow != null)
                return;

            _ordersWindow = GetComponent<CombatTacticOrdersWindow>();
            if (_ordersWindow == null)
                _ordersWindow = gameObject.AddComponent<CombatTacticOrdersWindow>();
        }

        private static TacticType ResolveDefaultTactic(FactionSO faction)
        {
            const TacticType preferred = TacticType.DisciplinedFire;
            if (TacticUnlockRules.IsUnlocked(faction, preferred))
                return preferred;

            if (faction?.startingTactics != null && faction.startingTactics.Length > 0)
                return faction.startingTactics[0];

            return preferred;
        }

        /// <summary>
        /// Non-interactive stand-in for the tactics window: a grimdark "TACTICAL PAUSE"
        /// band fades in over a brief time dilation, holds, and fades out before the next
        /// segment plays. Presentation-only pacing.
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
        /// grimdark kit (dark band + bone lettering) so it reads as the tactics window's
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

        /// <summary>Roster ids → piece definitions; null (with a warning per missing id) if any
        /// is unknown. "id:Ability" overrides the piece's grantedAbility via a runtime SO clone
        /// (testing aid — content ships real grants; the clone leaves assets untouched).</summary>
        private PieceDefinitionSO[] ResolveRoster(ContentDatabase database, string[] roster)
        {
            if (database?.Pieces == null || roster == null || roster.Length == 0)
                return null;

            var pieces = new PieceDefinitionSO[roster.Length];
            bool complete = true;
            for (int i = 0; i < roster.Length; i++)
            {
                string entry = roster[i];
                string id = entry;
                GrantedAbility? abilityOverride = null;
                int split = entry.IndexOf(':');
                if (split > 0)
                {
                    id = entry[..split];
                    if (Enum.TryParse(entry[(split + 1)..], out GrantedAbility parsed))
                        abilityOverride = parsed;
                    else
                        Debug.LogWarning($"[Combat3D] Unknown ability override in roster entry '{entry}' — ignored.", this);
                }

                var piece = database.Pieces.FirstOrDefault(p => p != null && p.id == id);
                if (piece == null)
                {
                    Debug.LogWarning($"[Combat3D] Roster piece id '{id}' not found in ContentDatabase.", this);
                    complete = false;
                    continue;
                }

                if (abilityOverride.HasValue && piece.grantedAbility != abilityOverride.Value)
                {
                    piece = Instantiate(piece); // runtime clone; the asset stays untouched
                    piece.name = $"{id} (demo {abilityOverride.Value})";
                    piece.grantedAbility = abilityOverride.Value;
                }

                pieces[i] = piece;
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
