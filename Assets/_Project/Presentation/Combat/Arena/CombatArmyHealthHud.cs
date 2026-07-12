using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Army-level health HUD for the 3D arena (arena spec §6: "army bars ... restyle chrome
    /// to game bible §6"): two opposing horizontal bars at the top of the screen — player
    /// left/blue, enemy right/red — each showing its side's remaining army HP fraction and
    /// draining toward the screen edge as the replay plays. Reuses the replay-driven
    /// <see cref="ArmyHealthReplayTracker"/> via <see cref="ArmyHealthBarPresenter"/> and the
    /// shared <see cref="CombatHudChromeBuilder"/> labels; the canvas itself is minimal
    /// screen-space chrome because the 2D arena's bar factory is Synty-prefab-coupled.
    /// Muted grimdark palette (ring-family blue/red) — VFX keep the saturation budget.
    /// </summary>
    public sealed class CombatArmyHealthHud : MonoBehaviour
    {
        [SerializeField] private CombatDirector director;

        // Ring-family side colors, lifted slightly for HUD-on-dark readability.
        private static readonly Color PlayerFill = new(0.30f, 0.42f, 0.60f, 1f);
        private static readonly Color EnemyFill = new(0.60f, 0.26f, 0.22f, 1f);
        private static readonly Color BarBackground = new(0.055f, 0.05f, 0.045f, 0.85f);

        private ArmyHealthBarPresenter _presenter;
        private GameObject _canvasRoot;
        private bool _subscribed;

        /// <summary>Build the HUD (once) and register every combat participant at full HP.
        /// Call after the arena presenter's InitializeArena with the same battlefield.</summary>
        public void Initialize(BattlefieldState battlefield)
        {
            if (_canvasRoot == null)
                BuildHud();

            _presenter.InitializeFromBattlefield(battlefield);
            Subscribe();
        }

        /// <summary>Externally driven mode (run flow): build the HUD chrome and hand back the
        /// inner <see cref="ArmyHealthBarPresenter"/> so the caller feeds it replay events
        /// itself. Put this component on a GameObject WITHOUT a CombatDirector, or the
        /// OnEnable self-subscription will double-apply every event.</summary>
        public ArmyHealthBarPresenter EnsurePresenter()
        {
            if (_canvasRoot == null)
                BuildHud();

            return _presenter;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || !isActiveAndEnabled)
                return;

            if (director == null)
                director = GetComponent<CombatDirector>();
            if (director == null)
                return;

            director.EventReplayed += OnEventReplayed;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (director != null)
                director.EventReplayed -= OnEventReplayed;
            _subscribed = false;
        }

        private void OnEventReplayed(CombatEvent combatEvent) =>
            _presenter?.HandleReplayEvent(combatEvent);

        private void BuildHud()
        {
            _canvasRoot = new GameObject("Combat3DArmyHud");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400; // under the result banner (500)

            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            // Opposing bars: full = both reach toward the center; damage drains each bar
            // outward toward its own screen edge, so the middle gap widens as armies bleed.
            var playerView = BuildBar(
                "PlayerArmyBar",
                anchorMinX: 0.055f, anchorMaxX: 0.47f,
                fillColor: PlayerFill,
                fillOrigin: Image.OriginHorizontal.Left);
            var enemyView = BuildBar(
                "EnemyArmyBar",
                anchorMinX: 0.53f, anchorMaxX: 0.945f,
                fillColor: EnemyFill,
                fillOrigin: Image.OriginHorizontal.Right);

            _presenter = gameObject.GetComponent<ArmyHealthBarPresenter>();
            if (_presenter == null)
                _presenter = gameObject.AddComponent<ArmyHealthBarPresenter>();
            _presenter.BindViews(playerView, enemyView);

            CombatHudChromeBuilder.AddSideLabel(playerView.transform, "YOUR FORCES", alignLeft: true);
            CombatHudChromeBuilder.AddSideLabel(enemyView.transform, "ENEMY FORCES", alignLeft: false);
        }

        private ArmyHealthBarView BuildBar(
            string name,
            float anchorMinX,
            float anchorMaxX,
            Color fillColor,
            Image.OriginHorizontal fillOrigin)
        {
            var barGo = new GameObject(name, typeof(RectTransform));
            barGo.transform.SetParent(_canvasRoot.transform, false);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(anchorMinX, 1f);
            barRect.anchorMax = new Vector2(anchorMaxX, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -42f); // room for the label above
            barRect.sizeDelta = new Vector2(0f, 16f);

            var background = barGo.AddComponent<Image>();
            background.sprite = UiWhiteSprite.Get();
            background.color = BarBackground;
            background.raycastTarget = false;

            // Named to dodge ArmyHealthBarView's anchor-based auto-resolve ("Fill") — the
            // Filled-image path is what lets the enemy bar mirror its drain direction.
            var fillGo = new GameObject("FillBar", typeof(RectTransform));
            fillGo.transform.SetParent(barGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            var fill = fillGo.AddComponent<Image>();
            fill.sprite = UiWhiteSprite.Get();
            fill.color = fillColor;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)fillOrigin;
            fill.fillAmount = 1f;

            var view = barGo.AddComponent<ArmyHealthBarView>();
            view.BindFillImage(fill);
            return view;
        }
    }
}
