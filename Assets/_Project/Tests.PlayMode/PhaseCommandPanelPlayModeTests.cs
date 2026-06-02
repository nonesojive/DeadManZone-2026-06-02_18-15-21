using System.Collections;
using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Presentation.Combat;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class PhaseCommandPanelPlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator ShowCommands_RendersEntryText()
        {
            _root = new GameObject("PhasePanelRoot");
            var panel = _root.AddComponent<PhaseCommandPanel>();

            var textGo = new GameObject("CommandText");
            textGo.transform.SetParent(_root.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            panel.InitializeForTests(text);

            var commands = new List<AvailableCommand>
            {
                new() { Type = CommandType.ChangeStance, SourcePieceId = "bunker_1", RequisitionCost = 0 },
                new() { Type = CommandType.SpendRequisitionBuff, SourcePieceId = "depot_1", RequisitionCost = 1 }
            };

            panel.ShowCommands(commands, CombatPhase.Deployment, 1, 2);
            yield return null;

            StringAssert.Contains("ChangeStance", text.text);
            StringAssert.Contains("SpendRequisitionBuff", text.text);
            StringAssert.Contains("Budget 1/2", text.text);
        }
    }
}
