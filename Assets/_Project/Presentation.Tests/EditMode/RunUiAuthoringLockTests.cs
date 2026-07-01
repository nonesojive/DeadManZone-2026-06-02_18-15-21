using DeadManZone.Presentation.Run;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class RunUiAuthoringLockTests
    {
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
    }
}
