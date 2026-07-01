using System.Collections;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Presentation.Combat;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class TacticPausePanelPlayModeTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp() => PlayModeTestHelpers.CleanupPersistentManagers();

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator Continue_DisabledWhenAuthorityInsufficient()
        {
            _root = new GameObject("TacticPanelRoot");
            var panel = _root.AddComponent<TacticPausePanel>();

            var authorityGo = new GameObject("Authority");
            authorityGo.transform.SetParent(_root.transform, false);
            var authorityText = authorityGo.AddComponent<TextMeshProUGUI>();

            var reasonGo = new GameObject("Reason");
            reasonGo.transform.SetParent(_root.transform, false);
            var reasonText = reasonGo.AddComponent<TextMeshProUGUI>();

            var buttonGo = new GameObject("Continue", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(_root.transform, false);
            var continueButton = buttonGo.GetComponent<Button>();

            panel.InitializeForTests(authorityText, reasonText, continueButton);

            var context = new CombatPauseContext
            {
                CheckpointIndex = 1,
                Trigger = new PauseTriggerContext
                {
                    CheckpointIndex = 1,
                    TriggeredBy = CombatSide.Enemy,
                    Threshold = 0.30f
                },
                Authority = 0,
                ActiveTactic = TacticType.DisciplinedFire,
                PendingSelectedTactic = TacticType.Advance,
                HasCommandPiece = false,
                AvailableAbilities = System.Array.Empty<AvailableCommand>()
            };

            panel.ShowPause(context);
            yield return null;

            Assert.IsFalse(continueButton.interactable);
            StringAssert.Contains("Insufficient Authority", reasonText.text);
        }

        [UnityTest]
        public IEnumerator Continue_EnabledForValidDisciplinedFire()
        {
            _root = new GameObject("TacticPanelRoot");
            var panel = _root.AddComponent<TacticPausePanel>();

            var authorityGo = new GameObject("Authority");
            authorityGo.transform.SetParent(_root.transform, false);
            var authorityText = authorityGo.AddComponent<TextMeshProUGUI>();

            var reasonGo = new GameObject("Reason");
            reasonGo.transform.SetParent(_root.transform, false);
            var reasonText = reasonGo.AddComponent<TextMeshProUGUI>();

            var buttonGo = new GameObject("Continue", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(_root.transform, false);
            var continueButton = buttonGo.GetComponent<Button>();

            panel.InitializeForTests(authorityText, reasonText, continueButton);

            panel.ShowPause(new CombatPauseContext
            {
                CheckpointIndex = 0,
                Authority = 2,
                ActiveTactic = TacticType.DisciplinedFire,
                HasCommandPiece = false,
                AvailableAbilities = System.Array.Empty<AvailableCommand>()
            });
            yield return null;

            Assert.IsTrue(continueButton.interactable);
            Assert.IsTrue(string.IsNullOrEmpty(reasonText.text));
        }
    }
}
