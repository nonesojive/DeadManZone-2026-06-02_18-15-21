using System.Collections;
using DeadManZone.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class VerticalSliceSavePlayModeTests
    {
        [UnityTest]
        public IEnumerator SaveAndLoad_PreservesGoldInPlayMode()
        {
            SaveManager.DeleteSave();

            var managerObject = new GameObject("RunManager_Test");
            var manager = managerObject.AddComponent<RunManager>();
            yield return null;

            manager.StartNewRun("iron_vanguard");
            manager.Orchestrator.State.Supplies = 55;
            manager.SaveAndExit();

            Object.Destroy(managerObject);
            yield return null;

            var reloadObject = new GameObject("RunManager_Reload");
            var reloaded = reloadObject.AddComponent<RunManager>();
            yield return null;

            Assert.IsTrue(reloaded.TryContinueRun());
            Assert.AreEqual(55, reloaded.State.Supplies);

            Object.Destroy(reloadObject);
            SaveManager.DeleteSave();
        }
    }
}
