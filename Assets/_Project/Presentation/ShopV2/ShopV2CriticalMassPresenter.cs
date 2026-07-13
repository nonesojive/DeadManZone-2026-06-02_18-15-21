using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;
using DeadManZone.Game;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>
    /// The always-visible CRITICAL MASS summary strip: one chip (icon + progress) per rule the
    /// board is actually engaged with, and a click opens the full CriticalMassDrawer.
    ///
    /// Binds authored children BY NAME, like every other ShopV2 presenter: `Chip_0..5` (each with
    /// `Icon` + `Label`) and `EmptyLabel`. It authors nothing — layout, sizing and hit surfaces
    /// live in the scene. Attach to `CriticalMassStrip`.
    /// </summary>
    public sealed class ShopV2CriticalMassPresenter : ShopV2PresenterBase
    {
        /// <summary>Matches the authored chip count. Beyond this it stops being a glance — that's the drawer's job.</summary>
        private const int MaxChips = 6;

        private static readonly Color Dim = new(0.65f, 0.65f, 0.65f, 0.85f);

        private sealed class Chip
        {
            public GameObject Root;
            public Image Icon;
            public TMP_Text Label;
            public ShopV2Tooltip Tooltip;
        }

        private readonly List<Chip> _chips = new();
        private GameObject _empty;
        private CriticalMassDrawerView _drawer;

        private void Awake()
        {
            var missing = new List<string>();

            for (int i = 0; i < MaxChips; i++)
            {
                var root = transform.Find($"Chip_{i}");
                if (root == null)
                {
                    missing.Add($"Chip_{i}");
                    continue;
                }

                _chips.Add(new Chip
                {
                    Root = root.gameObject,
                    Icon = root.Find("Icon")?.GetComponent<Image>(),
                    Label = root.Find("Label")?.GetComponent<TMP_Text>(),
                    Tooltip = root.GetComponent<ShopV2Tooltip>()
                });
            }

            var empty = transform.Find("EmptyLabel");
            if (empty != null)
                _empty = empty.gameObject;
            else
                missing.Add("EmptyLabel");

            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OpenDrawer);
            else
                missing.Add("Button");

            if (missing.Count > 0)
                Debug.LogWarning($"ShopV2CriticalMassPresenter: missing children: {string.Join(", ", missing)}", this);
        }

        private void OpenDrawer()
        {
            if (_drawer == null)
                _drawer = FindFirstObjectByType<CriticalMassDrawerView>(FindObjectsInactive.Include);

            _drawer?.Toggle();
        }

        protected override void Refresh(RunState state)
        {
            var manager = RunManager.Instance;
            if (manager == null || !manager.HasActiveRun)
                return;

            var boards = manager.Orchestrator?.GetBuildBoards();
            if (boards == null)
                return;

            // Active rules first, then the closest near-misses — the ones a single piece would tip.
            var entries = BuffStripEvaluator.Evaluate(boards)
                .OrderByDescending(e => e.IsActive)
                .ThenByDescending(e => e.CurrentCount)
                .Take(MaxChips)
                .ToList();

            var theme = UiThemeProvider.Current;
            var accent = theme != null ? theme.accentColor : Color.white;
            var library = ShopV2IconLibrary.Instance;

            for (int i = 0; i < _chips.Count; i++)
            {
                var chip = _chips[i];
                if (i >= entries.Count)
                {
                    chip.Root.SetActive(false);
                    continue;
                }

                var entry = entries[i];
                chip.Root.SetActive(true);

                // ShopV2's icon set covers every critical-mass rule tag. The legacy
                // CriticalMassIconsSO does not (no glyph for support/command/...) and silently
                // returns null — which is why the strip once rendered as bare numbers.
                if (chip.Icon != null)
                {
                    var sprite = library != null ? library.Get(entry.TagId) : null;
                    chip.Icon.sprite = sprite;
                    chip.Icon.enabled = sprite != null;
                    chip.Icon.color = entry.IsActive ? accent : Dim;
                }

                if (chip.Label != null)
                {
                    chip.Label.text = BuffStripEvaluator.FormatProgressLabel(entry);
                    chip.Label.color = entry.IsActive ? accent : Dim;
                }

                // The chip is a glance: "5/7". The tooltip is the answer to "5/7 of WHAT, and what
                // do I get?" — DetailText already carries exactly that, and the drawer uses the
                // same string, so the two can't disagree.
                chip.Tooltip?.SetContent(
                    entry.DisplayName.ToUpperInvariant()
                        + (entry.IsActive ? "  —  ACTIVE" : "  —  NOT YET"),
                    entry.DetailText);
            }

            if (_empty != null)
                _empty.SetActive(entries.Count == 0);
        }
    }
}
