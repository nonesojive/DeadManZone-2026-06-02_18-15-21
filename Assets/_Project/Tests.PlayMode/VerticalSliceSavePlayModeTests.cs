using System.Collections;
using DeadManZone.Core;
using DeadManZone.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class VerticalSliceSavePlayModeTests
    {
        [SetUp]
        public void SetUp() => PlayModeTestHelpers.CleanupPersistentManagers();

        [UnityTest]
        public IEnumerator SaveAndLoad_PreservesSuppliesInPlayMode()
        {
            var managerObject = new GameObject("RunManager_Test");
            var manager = managerObject.AddComponent<RunManager>();
            yield return null;

            manager.StartNewRun(FactionIds.IronVanguard);
            manager.Orchestrator.State.Supplies = 55;
            manager.SaveAndExit();

            Object.DestroyImmediate(managerObject);
            yield return null;

            var reloadObject = new GameObject("RunManager_Reload");
            var reloaded = reloadObject.AddComponent<RunManager>();
            yield return null;

            Assert.IsTrue(reloaded.TryContinueRun());
            Assert.AreEqual(55, reloaded.State.Supplies);

            Object.DestroyImmediate(reloadObject);
        }

        [TearDown]
        public void TearDown() => PlayModeTestHelpers.CleanupPersistentManagers();
    }
}
