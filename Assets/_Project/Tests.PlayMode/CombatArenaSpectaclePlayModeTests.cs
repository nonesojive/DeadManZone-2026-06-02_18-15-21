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

        [UnityTest]
        public IEnumerator HudAssets_LoadsHealthBarPrefab()
        {
            var hudAssets = Resources.Load<CombatHudAssetsSO>("DeadManZone/CombatHudAssets");
            Assert.NotNull(hudAssets, "CombatHudAssets.asset missing from Resources/DeadManZone/");
            Assert.NotNull(hudAssets.armyHealthBarPrefab, "armyHealthBarPrefab (HUD_Apocalypse_HealthBar_02)");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioSet_LoadsFromResources()
        {
            var audioSet = Resources.Load<CombatArenaAudioSetSO>("DeadManZone/CombatArenaAudioSet");
            Assert.NotNull(audioSet, "CombatArenaAudioSet.asset missing — run Pretty Combat Pass bootstrap menu");
            yield return null;
        }
    }
}
