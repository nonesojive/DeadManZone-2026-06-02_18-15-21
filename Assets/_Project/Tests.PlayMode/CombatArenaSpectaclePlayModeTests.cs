using System.Collections;
using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class CombatArenaSpectaclePlayModeTests
    {
        [UnityTest]
        public IEnumerator VfxSet_LoadsFromResources()
        {
            var vfxSet = Resources.Load<CombatArenaVfxSetSO>("DeadManZone/CombatArenaVfxSet");
            Assert.NotNull(vfxSet, "CombatArenaVfxSet.asset missing from Resources/DeadManZone/");
            Assert.NotNull(vfxSet.rifleMuzzle, "rifleMuzzle");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimationSet_LoadsFromResources()
        {
            var animationSet = Resources.Load<CombatArenaAnimationSetSO>("DeadManZone/CombatArenaAnimationSet");
            Assert.NotNull(animationSet, "CombatArenaAnimationSet.asset missing from Resources/DeadManZone/");
            Assert.NotNull(animationSet.rifleShoot, "rifleShoot");
            yield return null;
        }
    }
}
