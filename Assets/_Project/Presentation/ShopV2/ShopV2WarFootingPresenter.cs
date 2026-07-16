using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Binds the WarFooting readouts (army strength, salvage chance, dread) to RunState. Attach to `WarFooting`.</summary>
    public sealed class ShopV2WarFootingPresenter : ShopV2PresenterBase
    {
        private TMP_Text _armyStrength;
        private TMP_Text _salvageChance;
        private TMP_Text _dread;

        private void Awake()
        {
            var missing = new List<string>();
            _armyStrength = FindText("Val_ARMY STRENGTH", missing);
            _salvageChance = FindText("Val_SALVAGE CHANCE", missing);
            _dread = FindText("Val_DREAD", missing);

            if (missing.Count > 0)
                Debug.LogWarning($"ShopV2WarFootingPresenter: missing children: {string.Join(", ", missing)}", this);
        }

        protected override void Refresh(RunState state)
        {
            if (state == null)
                return;

            if (_armyStrength != null)
                _armyStrength.text = $"{PlayerStrengthDisplay()}  vs  {EnemyPreviewDisplay(state)}";

            if (_salvageChance != null)
                _salvageChance.text = $"{state.SalvageChancePercent}%";

            if (_dread != null)
                _dread.text = state.Dread.ToString();
        }

        private static string PlayerStrengthDisplay()
        {
            var manager = RunManager.Instance;
            if (manager == null || manager.Orchestrator == null || !manager.HasActiveRun)
                return "—";

            var board = manager.Orchestrator.GetCombatBoard();
            if (board == null)
                return "—";

            // Pass the full board set: combat evaluates synergy with the HQ board in scope, so
            // rating the combat board alone under-reports HQ-fed auras.
            return ArmyStrengthCalculator
                .Evaluate(board, manager.Orchestrator.GetBuildBoards())
                .EffectiveTotal
                .ToString();
        }

        private static string EnemyPreviewDisplay(RunState state)
        {
            int chosen = state.ChosenFightOption;
            if (state.FightOptions == null || chosen < 0 || chosen >= state.FightOptions.Count)
                return "—";

            return $"~{state.FightOptions[chosen].StrengthPreview}";
        }

        private TMP_Text FindText(string childName, List<string> missing)
        {
            var child = transform.Find(childName);
            var text = child != null ? child.GetComponent<TMP_Text>() : null;
            if (text == null)
                missing.Add(childName);
            return text;
        }
    }
}
