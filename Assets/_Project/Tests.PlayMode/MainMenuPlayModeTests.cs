using System.Collections;
using DeadManZone.Game;
using DeadManZone.Presentation.MainMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class MainMenuPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveManager.DeleteSave();
        }

        [UnityTest]
        public IEnumerator ContinueButton_HiddenWhenNoSave()
        {
            SceneManager.LoadScene(GameScenes.MainMenu);
            yield return null;

            var controller = Object.FindObjectOfType<MainMenuController>();
            Assert.NotNull(controller, "MainMenuController not found in MainMenu scene.");
            Assert.NotNull(controller.ContinueButtonRoot, "Continue button not wired on MainMenuController.");

            Assert.IsFalse(controller.ContinueButtonRoot.activeSelf,
                "Continue should be hidden when no save file exists.");
        }

        [TearDown]
        public void TearDown() => PlayModeTestHelpers.CleanupPersistentManagers();
    }
}
