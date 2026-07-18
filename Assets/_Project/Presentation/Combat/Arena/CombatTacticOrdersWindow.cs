using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// The interactive tactics window for the 3D demo's command pauses — the reference
    /// implementation for the main-build port. Runtime-built UI in the shared grimdark
    /// kit (canvas sort 450: above the army HUD at 400, under the result banner at 500).
    /// All rules stay in Core: this window edits a <see cref="CombatTacticOrdersDraft"/>
    /// (which defers every verdict to <see cref="TacticPauseValidator"/>) and hands the
    /// built PhaseCommands to the caller on RESUME. Targeted abilities route through
    /// <see cref="CombatTacticTargetPicker"/> (ground click → grid cell → validation).
    /// Mouse-only operable; Space/Escape are RESUME shortcuts.
    /// </summary>
    public sealed class CombatTacticOrdersWindow : MonoBehaviour
    {
        /// <summary>Everything one pause needs, snapshotted from the live run by the driver.</summary>
        public sealed class PauseContext
        {
            public int CheckpointIndex;
            public int Authority;
            public TacticType ActiveTactic;
            public bool HasCommandPiece;
            public TacticType[] StartingTactics;
            public IReadOnlyList<AvailableCommand> AvailableAbilities;
            public PauseTriggerContext Trigger;
            public CombatGridMapper Mapper;
            public Camera ArenaCamera;
            /// <summary>Mirrors what the Core executor will honor (alive enemy anchor at the cell).</summary>
            public Func<GridCoord, bool> IsValidAbilityTarget;
            /// <summary>Saved pause draft (run flow's SavePauseDraft) to restore on open.
            /// Targeted abilities are not restorable — the save schema has no target cells —
            /// so only the tactic and untargeted abilities re-seed.</summary>
            public TacticType? PendingSelectedTactic;
            public IReadOnlyList<GrantedAbility> PendingSelectedAbilities;

            /// <summary>2026-07-17 Oathborn transport tentpole (§2.5): fielded transports with
            /// live cargo — empty/null when none, in which case the DEPLOY ORDER section never
            /// appears (nothing to order).</summary>
            public IReadOnlyList<TransportOrderOption> TransportOrders;

            /// <summary>Any battlefield cell — "target a spot on the field", not a live-enemy
            /// gate like ability targeting.</summary>
            public Func<GridCoord, bool> IsValidTransportTargetCell;
        }

        /// <summary>Fires after any draft edit (tactic, queue, cancel). The run flow uses
        /// this to persist the pause draft; the demo leaves it unsubscribed.</summary>
        public event Action DraftChanged;

        private static readonly Color PanelBody = new(0.075f, 0.066f, 0.055f, 0.96f);
        private static readonly Color DimColor = new(0.02f, 0.02f, 0.025f, 0.55f);
        private static readonly Color SelectedLeather = new(0.30f, 0.245f, 0.16f, 0.98f);
        private static readonly Color SectionLabelColor = new(0.60f, 0.56f, 0.50f, 1f);

        private PauseContext _ctx;
        private CombatTacticOrdersDraft _draft;
        private CombatTacticTargetPicker _picker;
        private Action<List<PhaseCommand>> _onResume;
        private AvailableCommand _pickingCommand;

        // ---- resume-gated "SELECT TRANSPORT TARGET" state (round-2 playtest fix) ----
        // Clicking RESUME with an armed transport (has cargo, no target set yet) doesn't
        // resume immediately: the orders panel collapses and one prompt per un-targeted
        // transport chains in turn; only once every transport has a target (or was
        // explicitly skipped) does resume actually happen.
        private Queue<TransportOrderOption> _gateQueue;
        private TransportOrderOption _gateCurrent;
        private bool _gateSkipRequested;
        private GameObject _gatePromptRoot;
        private TMP_Text _gatePromptTitle;
        private Button _gateSkipButton;

        private GameObject _canvasRoot;
        private CanvasGroup _windowGroup;
        private Image _dimImage;
        private RectTransform _panelRect;
        private Vector2 _panelHomePosition;
        private TMP_Text _titleText;
        private TMP_Text _subtitleText;
        private TMP_Text _authorityText;
        private TMP_Text _reasonText;
        private GameObject _pickHintRoot;
        private RectTransform _tacticColumn;
        private RectTransform _abilityColumn;
        private RectTransform _queuedColumn;
        private Button _resumeButton;
        private Coroutine _rejectionRoutine;

        private readonly List<(Button button, Image background, TMP_Text label, TacticType tactic)> _tacticButtons = new();
        private readonly List<(Button button, TMP_Text label, AvailableCommand command)> _abilityButtons = new();

        public bool IsOpen { get; private set; }
        public bool IsPickingTarget => _picker != null && _picker.IsPicking;
        public CombatTacticOrdersDraft Draft => _draft;

        /// <summary>Verification seam: same accept path as a real battlefield click
        /// (<see cref="CombatTacticTargetPicker.TryPickWorldPoint"/>).</summary>
        public bool TryPickTargetAtWorldPoint(Vector3 worldPoint) =>
            _picker != null && _picker.TryPickWorldPoint(worldPoint);

        public void Show(PauseContext context, Action<List<PhaseCommand>> onResume)
        {
            _ctx = context ?? throw new ArgumentNullException(nameof(context));
            _onResume = onResume;
            _draft = new CombatTacticOrdersDraft(
                _ctx.ActiveTactic,
                _ctx.Authority,
                _ctx.CheckpointIndex,
                _ctx.HasCommandPiece,
                _ctx.StartingTactics);

            EnsureEventSystem();
            EnsureUi();
            EnsurePicker();
            _picker.ClearAll();

            _canvasRoot.SetActive(true);
            SetPickMode(false);
            IsOpen = true;

            _titleText.text = "TACTICAL PAUSE — ORDERS";
            _subtitleText.text = BuildSubtitle(_ctx);
            _reasonText.text = string.Empty;

            SeedFromPendingDraft();
            BuildTacticButtons();
            BuildAbilityButtons();
            Refresh();
        }

        /// <summary>Restore a previously saved pause draft: tactic + untargeted abilities.
        /// Every seed goes through the draft's normal validation, so a stale save (e.g. an
        /// ability whose piece died) is silently dropped rather than trusted.</summary>
        private void SeedFromPendingDraft()
        {
            if (_ctx.PendingSelectedTactic.HasValue)
                _draft.TrySelectTactic(_ctx.PendingSelectedTactic.Value, out _);

            if (_ctx.PendingSelectedAbilities == null || _ctx.AvailableAbilities == null)
                return;

            foreach (var ability in _ctx.PendingSelectedAbilities)
            {
                if (RequiresTarget(ability))
                    continue; // target cells are not persisted; the player re-picks

                var command = _ctx.AvailableAbilities.FirstOrDefault(c => c.Ability == ability);
                if (command != null)
                    _draft.TryQueueAbility(command, null, out _);
            }
        }

        public void Hide()
        {
            IsOpen = false;
            _picker?.ClearAll();
            if (_canvasRoot != null)
                _canvasRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen || IsPickingTarget)
                return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                TryResume();
        }

        private void OnDestroy()
        {
            if (_rejectionRoutine != null)
                StopCoroutine(_rejectionRoutine);

            // _canvasRoot is a detached scene-root object (see EnsureUi) so it no longer gets
            // auto-destroyed as a child of this transform.
            if (_canvasRoot != null)
                Destroy(_canvasRoot);
        }

        // ---------------------------------------------------------------- actions

        private void OnTacticClicked(TacticType tactic)
        {
            if (_draft.TrySelectTactic(tactic, out string reason))
                ClearRejection();
            else
                FlashRejection(reason);

            Refresh();
        }

        private void OnAbilityClicked(AvailableCommand command)
        {
            if (_draft.IsAbilityQueued(command.Ability))
                return; // button is disabled in this state; belt-and-braces

            if (RequiresTarget(command.Ability))
            {
                BeginTargetPick(command);
                return;
            }

            if (_draft.TryQueueAbility(command, null, out string reason))
                ClearRejection();
            else
                FlashRejection(reason);

            Refresh();
        }

        private void BeginTargetPick(AvailableCommand command)
        {
            if (_ctx.Mapper == null || _ctx.ArenaCamera == null)
            {
                FlashRejection("No battlefield mapper/camera");
                return;
            }

            _pickingCommand = command;
            SetPickMode(true);
            _picker.BeginPick(
                _ctx.Mapper,
                _ctx.ArenaCamera,
                _ctx.IsValidAbilityTarget,
                onPicked: cell =>
                {
                    SetPickMode(false);
                    if (_draft.TryQueueAbility(_pickingCommand, cell, out string reason))
                        ClearRejection();
                    else
                        FlashRejection(reason);
                    _pickingCommand = null;
                    Refresh();
                },
                onCancelled: () =>
                {
                    SetPickMode(false);
                    _pickingCommand = null;
                    Refresh();
                });
        }

        private void OnCancelQueuedClicked(int index)
        {
            _draft.RemoveQueuedAt(index);
            ClearRejection();
            Refresh();
        }

        private void TryResume()
        {
            if (!_draft.Validate(out string reason))
            {
                FlashRejection(reason);
                return;
            }

            if (TryBeginTransportTargetGate())
                return; // window collapses into the SELECT TRANSPORT TARGET prompt chain

            FinishResume();
        }

        private void FinishResume()
        {
            var commands = _draft.BuildCommands();
            Hide();
            _onResume?.Invoke(commands);
        }

        /// <summary>2026-07-17 round-2 playtest fix (owner-specified flow): after the player's
        /// normal tactics-window decisions, clicking RESUME gates on every ARMED transport
        /// (has cargo, no target set yet — the in-window DEPLOY row is still an optional
        /// pre-set that skips this gate for that transport) getting a target cell, one prompt
        /// at a time. Returns false (falls through to a normal resume) when there's nothing
        /// to gate on, e.g. no fielded transport has cargo, or the arena has no battlefield to
        /// target against.</summary>
        private bool TryBeginTransportTargetGate()
        {
            if (_ctx.Mapper == null || _ctx.ArenaCamera == null)
                return false;

            var armed = (_ctx.TransportOrders ?? (IReadOnlyList<TransportOrderOption>)Array.Empty<TransportOrderOption>())
                .Where(t => t.CargoCount > 0 && !_draft.HasTransportOrder(t.SourcePieceId))
                .ToList();
            if (armed.Count == 0)
                return false;

            _gateQueue = new Queue<TransportOrderOption>(armed);
            AdvanceTransportGate();
            return true;
        }

        private void AdvanceTransportGate()
        {
            if (_gateQueue == null || _gateQueue.Count == 0)
            {
                _gateQueue = null;
                _gateCurrent = null;
                FinishResume();
                return;
            }

            _gateCurrent = _gateQueue.Dequeue();
            ShowTransportGatePrompt(_gateCurrent);
        }

        /// <summary>2026-07-17 round-4 owner spec: ported from the pre-gate DEPLOY-button
        /// interaction (commit 59c6feb8's OnTransportOrderClicked) — the one the owner says
        /// "worked correctly". That flow was SetPickMode(true) + _picker.BeginPick with no other
        /// raycast-blocking element on screen, so CombatTacticTargetPicker.Update's own hover
        /// marker (gold=valid / dark red=invalid, tracks the mouse every frame) rendered fine.
        /// Round-2 added a full-screen CombatGroundClickRelay Image here so a synthetic
        /// EventSystem-dispatched click could also confirm a pick — but that Image, being
        /// raycastable and covering the whole screen, made EventSystem.IsPointerOverGameObject()
        /// return true everywhere, which is exactly the flag the picker's Update() checks before
        /// drawing the hover marker (and the flag that made almost every click land as a "valid"
        /// pick with zero visible aiming — the reported "click just starts combat, no targeting
        /// circle" bug). Deleted (see CombatGroundClickRelay.cs removal) — the picker's own
        /// Input.mousePosition poll already delivers real mouse clicks without it; a synthetic
        /// check can call TryPickTargetAtWorldPoint directly instead of faking a UI dispatch.</summary>
        private void ShowTransportGatePrompt(TransportOrderOption transport)
        {
            // Owner spec 2a: the orders window is FULLY GONE here (SetPickMode below hard-hides
            // it), not a dimmed ghost — only the gate prompt band + battlefield + hover marker
            // own the screen.
            SetPickMode(true);
            _pickHintRoot.SetActive(false);
            EnsureGatePromptUi();
            _gatePromptTitle.text = $"SELECT TARGET FOR {transport.SourceDisplayName.ToUpperInvariant()}";
            _gatePromptRoot.SetActive(true);

            _picker.ClearAll();
            BeginTransportGatePick(transport);
        }

        private void BeginTransportGatePick(TransportOrderOption transport)
        {
            _picker.BeginPick(
                _ctx.Mapper,
                _ctx.ArenaCamera,
                _ctx.IsValidTransportTargetCell,
                onPicked: cell =>
                {
                    if (_draft.TrySetTransportOrder(transport.SourcePieceId, cell, out string reason))
                    {
                        HideTransportGatePrompt();
                        AdvanceTransportGate();
                    }
                    else
                    {
                        // Owner spec 2c: an invalid pick gives feedback and STAYS in targeting —
                        // never silently advances/resumes on a rejected order.
                        FlashRejection(reason);
                        BeginTransportGatePick(transport);
                    }
                },
                onCancelled: () =>
                {
                    HideTransportGatePrompt();
                    if (_gateSkipRequested)
                    {
                        _gateSkipRequested = false;
                        AdvanceTransportGate();
                    }
                    else
                    {
                        AbortTransportGate();
                    }
                });
        }

        /// <summary>The prompt's one escape hatch: don't set a target for THIS transport (it
        /// simply advances & engages like any unit, per the in-window hint text) and move on
        /// to the next armed transport (or finish resuming). Never soft-locks the player behind
        /// a forced pick.</summary>
        private void OnSkipTransportGateClicked()
        {
            _gateSkipRequested = true;
            _picker.CancelPick();
        }

        private void HideTransportGatePrompt()
        {
            if (_gatePromptRoot != null)
                _gatePromptRoot.SetActive(false);
        }

        /// <summary>ESC/RMB during a gate pick abandons the whole chain and reopens the orders
        /// window (as opposed to SKIP, which only skips the current transport and continues
        /// the chain) — the player can hit RESUME again to re-trigger the gate.</summary>
        private void AbortTransportGate()
        {
            _gateQueue = null;
            _gateCurrent = null;
            SetPickMode(false);
            Refresh();
        }

        // ---------------------------------------------------------------- refresh

        private void Refresh()
        {
            int spent = _draft.TotalCost;
            _authorityText.text = $"AUTHORITY  {_draft.AuthorityRemaining} / {_draft.AuthorityTotal}" +
                                  (spent > 0 ? $"   (COMMITTED {spent})" : string.Empty);
            _authorityText.color = _draft.AuthorityRemaining < 0
                ? CombatGrimdarkSkin.DefeatRed
                : CombatGrimdarkSkin.Bone;

            foreach (var (button, background, label, tactic) in _tacticButtons)
            {
                bool selected = tactic == _draft.SelectedTactic;
                background.color = selected ? SelectedLeather : CombatGrimdarkSkin.ButtonLeather;
                if (button.interactable)
                    label.color = selected ? CombatGrimdarkSkin.Bone : CombatGrimdarkSkin.BodyText;
                label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }

            foreach (var (button, label, command) in _abilityButtons)
            {
                bool queued = _draft.IsAbilityQueued(command.Ability);
                button.interactable = !queued;
                label.text = BuildAbilityLabel(command, queued);
                label.color = queued
                    ? new Color(CombatGrimdarkSkin.BodyText.r, CombatGrimdarkSkin.BodyText.g, CombatGrimdarkSkin.BodyText.b, 0.45f)
                    : CombatGrimdarkSkin.BodyText;
            }

            RebuildQueuedList();
            var markers = _draft.Queued
                .Where(q => q.TargetCell.HasValue)
                .Select(q => q.TargetCell.Value)
                .ToList();
            markers.AddRange(_draft.TransportOrders.Values);
            _picker.SetPendingMarkers(markers);

            _resumeButton.interactable = _draft.Validate(out _);
            DraftChanged?.Invoke();
        }

        private void RebuildQueuedList()
        {
            DestroyChildren(_queuedColumn);

            // Row 0 is always the doctrine order (submitted as the SetTactic command).
            AddQueuedRow(
                $"DOCTRINE — {FormatTactic(_draft.SelectedTactic)}",
                cancelIndex: null);

            for (int i = 0; i < _draft.Queued.Count; i++)
            {
                var queued = _draft.Queued[i];
                string target = queued.TargetCell.HasValue
                    ? $" @ {queued.TargetCell.Value.X},{queued.TargetCell.Value.Y}"
                    : string.Empty;
                AddQueuedRow(
                    $"{FormatAbility(queued.Command.Ability)}{target}  ({queued.Command.RequisitionCost}A)",
                    cancelIndex: i);
            }

            // 2026-07-17 round-3 playtest fix: no in-window DEPLOY row anymore — a transport
            // order is only ever set by the resume-gated SELECT TRANSPORT TARGET prompt
            // (TryBeginTransportTargetGate), which fires after this window has already closed.
        }

        // ---------------------------------------------------------------- pick mode

        /// <summary>2026-07-17 round-4 owner spec: while picking, the orders window is FULLY
        /// GONE (SetActive(false) on the whole "Window" group) — not a dimmed ghost sitting
        /// behind the battlefield. The dim image also clears so battlefield clicks/hover reach
        /// the ground, and a hint band explains the controls. Restored (SetActive(true)) cleanly
        /// on pick or cancel — right-click/ESC during a pick routes to onCancelled, which calls
        /// this with picking=false, so ESCing back to orders un-hides the window properly.</summary>
        private void SetPickMode(bool picking)
        {
            _windowGroup.gameObject.SetActive(!picking);
            _dimImage.raycastTarget = !picking;
            _dimImage.color = picking ? Color.clear : DimColor;
            _pickHintRoot.SetActive(picking);
        }

        // ---------------------------------------------------------------- rejection feedback

        private void FlashRejection(string reason)
        {
            _reasonText.text = string.IsNullOrEmpty(reason) ? "ORDER REFUSED" : reason.ToUpperInvariant();
            if (_rejectionRoutine != null)
                StopCoroutine(_rejectionRoutine);
            _rejectionRoutine = StartCoroutine(RejectionFlashRoutine());
        }

        private void ClearRejection()
        {
            if (_rejectionRoutine != null)
            {
                StopCoroutine(_rejectionRoutine);
                _rejectionRoutine = null;
                _panelRect.anchoredPosition = _panelHomePosition;
            }

            _reasonText.text = string.Empty;
        }

        private IEnumerator RejectionFlashRoutine()
        {
            const float duration = 0.3f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float falloff = 1f - t / duration;
                _panelRect.anchoredPosition = _panelHomePosition +
                    new Vector2(Mathf.Sin(t * 70f) * 7f * falloff, 0f);
                _reasonText.color = Color.Lerp(
                    CombatGrimdarkSkin.DefeatRed,
                    new Color(1f, 0.45f, 0.38f, 1f),
                    Mathf.PingPong(t * 6f, 1f));
                yield return null;
            }

            _panelRect.anchoredPosition = _panelHomePosition;
            _reasonText.color = CombatGrimdarkSkin.DefeatRed;
            _rejectionRoutine = null;
        }

        // ---------------------------------------------------------------- UI construction

        private void EnsurePicker()
        {
            if (_picker == null)
                _picker = gameObject.GetComponent<CombatTacticTargetPicker>() ??
                          gameObject.AddComponent<CombatTacticTargetPicker>();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.name = "EventSystem";
        }

        private void EnsureUi()
        {
            if (_canvasRoot != null)
                return;

            _canvasRoot = new GameObject("CombatTacticOrdersCanvas");
            // 2026-07-17 round-2 playtest fix: must be a scene-ROOT canvas, not parented under
            // this window's own transform. This window lives inside the arena's existing Canvas
            // hierarchy (Canvas/RunScene/CombatPanel) — parenting under it made the new Canvas
            // component NESTED (Canvas.isRootCanvas == false). Unity only auto-sizes a Screen
            // Space - Overlay canvas's RectTransform to the real screen for ROOT canvases; a
            // nested one keeps whatever RectTransform it was given (Unity's default: 100x100,
            // anchored center). Center-anchored fixed-size children (the orders panel, the
            // prompt bands) still LOOKED right, because they only depend on the parent's anchor
            // POINT (still screen-center) — but every StretchFull() child (BattlefieldDim, the
            // pick-hint band, and critically GroundClickRelay, the real ground-click catcher)
            // silently shrank to a ~100x100px hotbox in the dead center of the screen. A real
            // click anywhere else on the battlefield hit nothing at all — no reject feedback,
            // just silence. Detaching to the scene root makes this a genuine root canvas so its
            // rect (and everything stretched to it) actually covers the screen. OnDestroy below
            // now destroys it explicitly since it's no longer a child that auto-cleans up.
            _canvasRoot.transform.SetParent(null, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450; // above army HUD (400), under result banner (500)
            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _canvasRoot.AddComponent<GraphicRaycaster>();

            // Battlefield dim (blocks clicks while the window owns the screen).
            var dimGo = new GameObject("BattlefieldDim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(_canvasRoot.transform, false);
            StretchFull(dimGo.GetComponent<RectTransform>());
            _dimImage = dimGo.GetComponent<Image>();
            _dimImage.color = DimColor;

            // Window group: panel + everything interactable.
            var groupGo = new GameObject("Window", typeof(RectTransform), typeof(CanvasGroup));
            groupGo.transform.SetParent(_canvasRoot.transform, false);
            StretchFull(groupGo.GetComponent<RectTransform>());
            _windowGroup = groupGo.GetComponent<CanvasGroup>();

            _panelRect = CreatePanel(groupGo.transform);
            _panelHomePosition = _panelRect.anchoredPosition;

            BuildPickHint();

            _canvasRoot.SetActive(false);
        }

        /// <summary>2026-07-17 round-2 playtest fix (owner-specified flow): the collapsed-window
        /// prompt naming the armed transport, with its one escape hatch — SKIP DEPLOY (the
        /// owner asked this be flagged for their review; see the shipped report).</summary>
        private void EnsureGatePromptUi()
        {
            if (_gatePromptRoot != null)
                return;

            _gatePromptRoot = new GameObject("TransportGatePrompt", typeof(RectTransform));
            _gatePromptRoot.transform.SetParent(_canvasRoot.transform, false);
            StretchFull(_gatePromptRoot.GetComponent<RectTransform>());

            CombatGrimdarkSkin.AddBand(_gatePromptRoot.transform, 0.88f, 0.965f, "TransportGateBand");
            _gatePromptTitle = CreateLabel(_gatePromptRoot.transform, "TransportGateTitle",
                "SELECT TRANSPORT TARGET", 24f,
                new Vector2(0f, 0.915f), new Vector2(1f, 0.965f), TextAlignmentOptions.Center);
            CombatGrimdarkSkin.StyleTitle(_gatePromptTitle);

            var body = CreateLabel(_gatePromptRoot.transform, "TransportGateBody",
                "LEFT CLICK A BATTLEFIELD CELL — DEPLOY THERE       RIGHT CLICK / ESC — BACK TO ORDERS", 13f,
                new Vector2(0f, 0.885f), new Vector2(1f, 0.915f), TextAlignmentOptions.Center);
            body.characterSpacing = 2f;
            CombatGrimdarkSkin.StyleBody(body);

            _gateSkipButton = CreateButton(_gatePromptRoot.transform, "SkipDeployLink",
                "SKIP DEPLOY — ADVANCE & ENGAGE NORMALLY",
                new Vector2(0.5f, 0.855f), new Vector2(380f, 28f), 12f);
            _gateSkipButton.onClick.AddListener(OnSkipTransportGateClicked);

            _gatePromptRoot.SetActive(false);
        }

        private RectTransform CreatePanel(Transform parent)
        {
            var panelGo = new GameObject("OrdersPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            var rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(860f, 620f);
            panelGo.GetComponent<Image>().color = PanelBody;

            // Header band + title + subtitle (FIGHT-banner language).
            CombatGrimdarkSkin.AddBand(panelGo.transform, 0.875f, 1f, "HeaderBand");
            _titleText = CreateLabel(panelGo.transform, "TitleLabel", "TACTICAL PAUSE — ORDERS", 28f,
                new Vector2(0f, 0.905f), new Vector2(1f, 1f), TextAlignmentOptions.Center);
            CombatGrimdarkSkin.StyleTitle(_titleText);
            _subtitleText = CreateLabel(panelGo.transform, "SubtitleLabel", "", 14f,
                new Vector2(0f, 0.875f), new Vector2(1f, 0.91f), TextAlignmentOptions.Center);
            _subtitleText.characterSpacing = 3f;
            CombatGrimdarkSkin.StyleBody(_subtitleText);

            _authorityText = CreateLabel(panelGo.transform, "AuthorityLabel", "", 19f,
                new Vector2(0f, 0.805f), new Vector2(1f, 0.865f), TextAlignmentOptions.Center);
            _authorityText.fontStyle = FontStyles.Bold;
            _authorityText.characterSpacing = 2f;

            // Left column: doctrine. Right column: abilities + queued orders.
            _tacticColumn = CreateColumn(panelGo.transform, "TacticColumn",
                new Vector2(0.035f, 0.17f), new Vector2(0.475f, 0.80f));
            _abilityColumn = CreateColumn(panelGo.transform, "AbilityColumn",
                new Vector2(0.525f, 0.50f), new Vector2(0.965f, 0.80f));
            _queuedColumn = CreateColumn(panelGo.transform, "QueuedColumn",
                new Vector2(0.525f, 0.17f), new Vector2(0.965f, 0.47f));

            _reasonText = CreateLabel(panelGo.transform, "ReasonLabel", "", 15f,
                new Vector2(0.03f, 0.115f), new Vector2(0.97f, 0.165f), TextAlignmentOptions.Center);
            _reasonText.fontStyle = FontStyles.Italic;
            _reasonText.color = CombatGrimdarkSkin.DefeatRed;

            // Footer band + RESUME.
            CombatGrimdarkSkin.AddBand(panelGo.transform, 0f, 0.11f, "FooterBand");
            _resumeButton = CreateButton(panelGo.transform, "ResumeButton", "RESUME COMBAT",
                new Vector2(0.5f, 0.055f), new Vector2(240f, 46f), 18f);
            _resumeButton.onClick.AddListener(TryResume);
            var shortcutHint = CreateLabel(panelGo.transform, "ShortcutHint", "SPACE / ESC", 11f,
                new Vector2(0.72f, 0.02f), new Vector2(0.98f, 0.09f), TextAlignmentOptions.Right);
            shortcutHint.color = SectionLabelColor;
            shortcutHint.characterSpacing = 2f;

            return rect;
        }

        private void BuildPickHint()
        {
            _pickHintRoot = new GameObject("TargetPickHint", typeof(RectTransform));
            _pickHintRoot.transform.SetParent(_canvasRoot.transform, false);
            StretchFull(_pickHintRoot.GetComponent<RectTransform>());

            CombatGrimdarkSkin.AddBand(_pickHintRoot.transform, 0.88f, 0.965f, "PickHintBand");
            var title = CreateLabel(_pickHintRoot.transform, "PickHintTitle",
                "SELECT TARGET", 26f,
                new Vector2(0f, 0.915f), new Vector2(1f, 0.965f), TextAlignmentOptions.Center);
            CombatGrimdarkSkin.StyleTitle(title);
            var body = CreateLabel(_pickHintRoot.transform, "PickHintBody",
                "LEFT CLICK — CONFIRM       RIGHT CLICK / ESC — CANCEL", 14f,
                new Vector2(0f, 0.885f), new Vector2(1f, 0.915f), TextAlignmentOptions.Center);
            body.characterSpacing = 3f;
            CombatGrimdarkSkin.StyleBody(body);

            _pickHintRoot.SetActive(false);
        }

        private void BuildTacticButtons()
        {
            DestroyChildren(_tacticColumn);
            _tacticButtons.Clear();

            AddSectionLabel(_tacticColumn, "DOCTRINE");

            foreach (TacticType tactic in Enum.GetValues(typeof(TacticType)))
            {
                // Single source of truth for the lock verdict — TacticPauseValidator (Core) is
                // what actually gates RESUME; this label must never drift from it (owner rule
                // 2026-07-17: Advance + Hold The Line always unlocked, see its doc comment).
                bool unlocked = TacticPauseValidator.IsTacticUnlocked(_ctx.StartingTactics, tactic);

                string label = FormatTactic(tactic) + (unlocked ? string.Empty : "  — LOCKED");
                var button = CreateColumnButton(_tacticColumn, $"TacticButton_{tactic}", label, 44f);
                var background = button.GetComponent<Image>();
                var text = button.GetComponentInChildren<TMP_Text>();
                button.interactable = unlocked;
                if (!unlocked)
                    text.color = new Color(
                        CombatGrimdarkSkin.BodyText.r,
                        CombatGrimdarkSkin.BodyText.g,
                        CombatGrimdarkSkin.BodyText.b, 0.35f);

                var captured = tactic;
                button.onClick.AddListener(() => OnTacticClicked(captured));
                _tacticButtons.Add((button, background, text, tactic));
            }
        }

        private void BuildAbilityButtons()
        {
            DestroyChildren(_abilityColumn);
            _abilityButtons.Clear();

            AddSectionLabel(_abilityColumn, "UNIT ABILITIES");

            if (_ctx.AvailableAbilities == null || _ctx.AvailableAbilities.Count == 0)
            {
                var none = CreateLabel(_abilityColumn, "NoAbilities", "NO ABILITIES AVAILABLE", 13f,
                    Vector2.zero, Vector2.one, TextAlignmentOptions.Left);
                none.color = SectionLabelColor;
                SetColumnItemHeight(none.rectTransform, 30f);
                return;
            }

            foreach (var command in _ctx.AvailableAbilities)
            {
                var button = CreateColumnButton(
                    _abilityColumn,
                    $"AbilityButton_{command.Ability}",
                    BuildAbilityLabel(command, queued: false),
                    44f);
                var captured = command;
                button.onClick.AddListener(() => OnAbilityClicked(captured));
                _abilityButtons.Add((button, button.GetComponentInChildren<TMP_Text>(), command));
            }
        }

        private void AddQueuedRow(string text, int? cancelIndex)
        {
            if (!cancelIndex.HasValue)
            {
                AddQueuedRow(text, "QueuedRow_Doctrine", null, null);
                return;
            }

            int captured = cancelIndex.Value;
            AddQueuedRow(text, $"QueuedRow_{captured}", $"QueuedCancel_{captured}", () => OnCancelQueuedClicked(captured));
        }

        private void AddQueuedRow(string text, string rowName, string cancelButtonName, Action onCancel)
        {
            if (_queuedColumn.childCount == 0)
                AddSectionLabel(_queuedColumn, "QUEUED ORDERS");

            var rowGo = new GameObject(rowName, typeof(RectTransform), typeof(Image));
            rowGo.transform.SetParent(_queuedColumn, false);
            rowGo.GetComponent<Image>().color = new Color(0.10f, 0.09f, 0.075f, 0.9f);
            SetColumnItemHeight(rowGo.GetComponent<RectTransform>(), 34f);

            var label = CreateLabel(rowGo.transform, "RowLabel", text, 13f,
                new Vector2(0.03f, 0f), new Vector2(onCancel != null ? 0.72f : 0.97f, 1f),
                TextAlignmentOptions.Left);
            label.color = CombatGrimdarkSkin.BodyText;

            if (onCancel == null)
                return;

            var cancel = CreateButton(rowGo.transform, cancelButtonName, "CANCEL",
                new Vector2(0.86f, 0.5f), new Vector2(86f, 26f), 11f);
            cancel.onClick.AddListener(() => onCancel());
        }

        // ---------------------------------------------------------------- helpers

        private static bool RequiresTarget(GrantedAbility ability) =>
            ability is GrantedAbility.MortarShot or GrantedAbility.CannonBlast or GrantedAbility.RollingBarrage;

        private static string BuildSubtitle(PauseContext ctx)
        {
            if (ctx.CheckpointIndex == 0)
                return "OPENING ORDERS — LINES HOLD UNTIL COMMITTED";

            var trigger = ctx.Trigger;
            if (trigger == null)
                return "MID-FIGHT ORDERS";

            string side = trigger.TriggeredBy == CombatSide.Player ? "YOUR" : "ENEMY";
            return $"{side} FORCES AT {(int)(trigger.Threshold * 100)}% — ORDERS HOLD";
        }

        private static string BuildAbilityLabel(AvailableCommand command, bool queued)
        {
            string state = queued ? "  — QUEUED" : string.Empty;
            // Run-flow instance ids are GUIDs — only readable names reach the screen.
            string source = string.IsNullOrEmpty(command.SourceDisplayName)
                ? command.SourcePieceId
                : command.SourceDisplayName;
            return $"{FormatAbility(command.Ability)}  ·  {source}  ({command.RequisitionCost}A){state}";
        }

        private static string FormatTactic(TacticType tactic) =>
            tactic switch
            {
                TacticType.DisciplinedFire => "DISCIPLINED FIRE",
                TacticType.Advance => "ADVANCE",
                TacticType.StandGround => "HOLD THE LINE",
                TacticType.ProtectSupport => "PROTECT SUPPORT",
                _ => tactic.ToString().ToUpperInvariant()
            };

        private static string FormatAbility(GrantedAbility ability) =>
            ability switch
            {
                GrantedAbility.MortarShot => "MORTAR SHOT",
                GrantedAbility.ShieldAllies => "SHIELD ALLIES",
                GrantedAbility.CannonBlast => "CANNON BLAST",
                GrantedAbility.RollingBarrage => "ROLLING BARRAGE",
                _ => ability.ToString().ToUpperInvariant()
            };

        /// <summary>Destroy() is end-of-frame; detach first so same-frame childCount
        /// checks and layout rebuilds never see the corpses.</summary>
        private static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateColumn(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            // Must control height: with childControlHeight=false Unity ignores the
            // LayoutElement min/preferred heights and rows keep their default 100px rect.
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        private static void AddSectionLabel(Transform column, string text)
        {
            var label = CreateLabel(column, $"Section_{text}", text, 13f,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Left);
            label.color = SectionLabelColor;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 4f;
            SetColumnItemHeight(label.rectTransform, 24f);
        }

        private static void SetColumnItemHeight(RectTransform rect, float height)
        {
            var element = rect.gameObject.GetComponent<LayoutElement>() ??
                          rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }

        private static Button CreateColumnButton(Transform column, string name, string label, float height)
        {
            var button = CreateButton(column, name, label, Vector2.zero, new Vector2(0f, height), 15f);
            var rect = button.GetComponent<RectTransform>();
            SetColumnItemHeight(rect, height);
            return button;
        }

        private static Button CreateButton(
            Transform parent, string name, string label, Vector2 anchor, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var text = CreateLabel(go.transform, "Label", label, fontSize,
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), TextAlignmentOptions.Center);
            text.raycastTarget = false;
            text.color = CombatGrimdarkSkin.Bone;

            var button = go.GetComponent<Button>();
            CombatGrimdarkSkin.StyleButton(button);
            var colors = button.colors;
            colors.disabledColor = new Color(0.55f, 0.53f, 0.50f, 0.6f);
            button.colors = colors;
            return button;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = CombatGrimdarkSkin.Bone;
            return label;
        }
    }
}
