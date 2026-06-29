using DeadManZone.Presentation.Run;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class RunUiAuthoringLockTests
    {
        [Test]
        public void ShouldPreserve_ReturnsFalse_WhenNoLock()
        {
            var buildPanel = new GameObject("BuildPanel");

            try
            {
                Assert.IsFalse(RunUiAuthoringLock.ShouldPreserve(buildPanel.transform));
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void ShouldPreserve_ReturnsTrue_WhenLockEnabled()
        {
            var buildPanel = new GameObject("BuildPanel");
            var authoringLock = buildPanel.AddComponent<RunUiAuthoringLock>();
            authoringLock.PreserveSceneAuthoring = true;

            try
            {
                Assert.IsTrue(RunUiAuthoringLock.ShouldPreserve(buildPanel.transform));
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void ShouldPreserve_ReturnsFalse_WhenLockDisabled()
        {
            var buildPanel = new GameObject("BuildPanel");
            var authoringLock = buildPanel.AddComponent<RunUiAuthoringLock>();
            authoringLock.PreserveSceneAuthoring = false;

            try
            {
                Assert.IsFalse(RunUiAuthoringLock.ShouldPreserve(buildPanel.transform));
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void ShouldSkipVisualMigration_ReturnsTrue_InEditMode_WhenLockEnabled()
        {
            var buildPanel = new GameObject("BuildPanel");
            buildPanel.AddComponent<RunUiAuthoringLock>();

            try
            {
                Assert.IsTrue(RunUiAuthoringLock.ShouldSkipVisualMigration(buildPanel.transform, isPlaying: false));
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void FindBuildPanel_ReturnsShopScene_WhenDescendantOfShopScene()
        {
            var shopScene = new GameObject("ShopScene");
            var child = new GameObject("MainRow");
            child.transform.SetParent(shopScene.transform, false);

            try
            {
                Assert.AreSame(shopScene.transform, RunUiAuthoringLock.FindBuildPanel(child.transform));
            }
            finally
            {
                Object.DestroyImmediate(shopScene);
            }
        }

        [Test]
        public void ShouldSkipVisualMigration_ReturnsTrue_InPlayMode_WhenLockEnabled()
        {
            var buildPanel = new GameObject("BuildPanel");
            buildPanel.AddComponent<RunUiAuthoringLock>();

            try
            {
                Assert.IsTrue(RunUiAuthoringLock.ShouldSkipVisualMigration(buildPanel.transform, isPlaying: true));
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }

        [Test]
        public void EnsureOn_AddsLockWithPreserveEnabled()
        {
            var buildPanel = new GameObject("BuildPanel");

            try
            {
                var authoringLock = RunUiAuthoringLock.EnsureOn(buildPanel.transform);

                Assert.NotNull(authoringLock);
                Assert.IsTrue(authoringLock.PreserveSceneAuthoring);
                Assert.AreSame(authoringLock, buildPanel.GetComponent<RunUiAuthoringLock>());
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }
    }
}
