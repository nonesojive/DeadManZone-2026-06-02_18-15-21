using DeadManZone.Presentation.Combat;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Combat playback speed toggle (2026-07-12 playtest request): a single kit-styled
    /// button cycling 1x / 2x / 4x. Scales Time.timeScale — event pacing, movement,
    /// animation and VFX all stay in lockstep (scaling only the director's tick delay
    /// desyncs walkers from their sim anchors). The scale applies ONLY while the
    /// director is actually replaying a segment: Update polls <see cref="CombatDirector.IsPlaying"/>
    /// so tactical pauses, the battle report and the shop always run at 1x with no
    /// event bookkeeping to leak — a stuck 2x world is the failure mode this design
    /// exists to make impossible. Every fight opens at 1x — the choice deliberately
    /// does NOT persist (a fight STARTING at 4x reads as a bug, not a preference).
    /// Lives on its OWN top-level overlay canvas (nesting under any UI transform
    /// inherits the parent rect — see CombatArmyHealthHud).
    /// </summary>
    public sealed class CombatPlaybackSpeedControl : MonoBehaviour
    {
        private static readonly float[] Speeds = { 1f, 2f, 4f };

        private CombatDirector _director;
        private TMP_Text _label;
        private int _speedIndex;
        private bool _scaling;

        public void Configure(CombatDirector director) => _director = director;

        private void Awake()
        {
            BuildUi();
            RefreshLabel();
        }

        private void OnEnable()
        {
            // Every fight opens at 1x (2026-07-12 playtest: persisting the choice made
            // combat START fast, which reads as a bug, not a preference). The button
            // only holds its speed within the current fight.
            _speedIndex = 0;
            RefreshLabel();
        }

        private void Update()
        {
            bool shouldScale = _director != null && _director.IsPlaying;
            if (shouldScale == _scaling && (!shouldScale || Mathf.Approximately(Time.timeScale, Speeds[_speedIndex])))
                return;

            _scaling = shouldScale;
            Time.timeScale = shouldScale ? Speeds[_speedIndex] : 1f;
        }

        private void OnDisable()
        {
            if (_scaling)
                Time.timeScale = 1f;
            _scaling = false;
        }

        private void Cycle()
        {
            _speedIndex = (_speedIndex + 1) % Speeds.Length;
            RefreshLabel();
            if (_scaling)
                Time.timeScale = Speeds[_speedIndex];
        }

        private void RefreshLabel()
        {
            if (_label != null)
                _label.text = $"SPEED {Speeds[_speedIndex]:0}×";
        }

        private void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // above the army HUD bars, below modal windows
            gameObject.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            var buttonGo = new GameObject("SpeedButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(transform, false);
            var rect = (RectTransform)buttonGo.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -64f); // tucked under the army bars
            rect.sizeDelta = new Vector2(132f, 36f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 18f;

            var button = buttonGo.GetComponent<Button>();
            button.onClick.AddListener(Cycle);
            CombatGrimdarkSkin.StyleButton(button);
        }
    }
}
