using System.Collections.Generic;
using DeadManZone.Core.Run;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Binds the CommandBar (resource chips, fight title, dread clock) to RunState. Attach to `CommandBar`.</summary>
    public sealed class ShopV2CommandBarPresenter : ShopV2PresenterBase
    {
        // Display ceiling for the dread meter fill only — NOT a rule; dread itself is uncapped in Core.
        private const float DreadMeterDisplayCeiling = 12f;

        private TMP_Text _suppliesValue;
        private TMP_Text _manpowerValue;
        private TMP_Text _authorityValue;
        private TMP_Text _fightTitle;
        private TMP_Text _dreadValue;
        private RectTransform _meterBg;
        private RectTransform _meterFill;
        private bool _bound;

        private void Awake()
        {
            var missing = new List<string>();

            _suppliesValue = FindText("Chip_SUPPLIES/Value", missing);
            _manpowerValue = FindText("Chip_MANPOWER/Value", missing);
            _authorityValue = FindText("Chip_AUTHORITY/Value", missing);
            _fightTitle = FindText("FightTitle", missing);
            _dreadValue = FindText("DreadClock/Value", missing);

            var bg = transform.Find("DreadClock/MeterBg");
            _meterBg = bg as RectTransform;
            if (_meterBg == null)
                missing.Add("DreadClock/MeterBg");

            var fill = transform.Find("DreadClock/MeterBg/MeterFill")
                ?? transform.Find("DreadClock/MeterFill");
            _meterFill = fill as RectTransform;
            if (_meterFill == null)
                missing.Add("DreadClock/MeterFill");

            _bound = true;
            if (missing.Count > 0)
                Debug.LogWarning($"ShopV2CommandBarPresenter: missing children: {string.Join(", ", missing)}", this);
        }

        protected override void Refresh(RunState state)
        {
            if (!_bound || state == null)
                return;

            if (_suppliesValue != null)
                _suppliesValue.text = state.Supplies.ToString();
            if (_manpowerValue != null)
                _manpowerValue.text = state.Manpower.ToString();
            if (_authorityValue != null)
                _authorityValue.text = state.Authority.ToString();

            if (_fightTitle != null)
                // FightIndex is already the human-facing fight number (see CombatFightBanner, RunEndOverlayView).
                _fightTitle.text = $"FIGHT {state.FightIndex}  —  {FactionDisplay(state.FactionId)}";

            if (_dreadValue != null)
                _dreadValue.text = state.Dread.ToString();

            if (_meterBg != null && _meterFill != null)
            {
                float t = Mathf.Clamp01(state.Dread / DreadMeterDisplayCeiling);
                _meterFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _meterBg.rect.width * t);
            }
        }

        private static string FactionDisplay(string factionId) =>
            string.IsNullOrEmpty(factionId) ? string.Empty : factionId.Replace('_', ' ').ToUpperInvariant();

        private TMP_Text FindText(string path, List<string> missing)
        {
            var child = transform.Find(path);
            var text = child != null ? child.GetComponent<TMP_Text>() : null;
            if (text == null)
                missing.Add(path);
            return text;
        }
    }
}
