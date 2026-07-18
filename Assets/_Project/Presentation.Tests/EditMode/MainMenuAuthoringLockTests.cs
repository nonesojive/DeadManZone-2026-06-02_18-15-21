using DeadManZone.Presentation.MainMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class MainMenuAuthoringLockTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void ShouldPreserve_InvalidScene_ReturnsFalse()
        {
            Assert.IsFalse(MainMenuAuthoringLock.ShouldPreserve(default(Scene)));
        }

        [Test]
        public void ShouldPreserve_SceneWithLock_ReturnsTrue()
        {
            _root = new GameObject("Canvas");
            _root.AddComponent<MainMenuAuthoringLock>();

            Assert.IsTrue(MainMenuAuthoringLock.ShouldPreserve(_root.scene));
        }

        [Test]
        public void ShouldPreserve_LockDisabledViaFlag_ReturnsFalse()
        {
            _root = new GameObject("Canvas");
            var authoringLock = _root.AddComponent<MainMenuAuthoringLock>();
            authoringLock.PreserveSceneAuthoring = false;

            Assert.IsFalse(MainMenuAuthoringLock.ShouldPreserve(_root.scene));
        }
    }
}
