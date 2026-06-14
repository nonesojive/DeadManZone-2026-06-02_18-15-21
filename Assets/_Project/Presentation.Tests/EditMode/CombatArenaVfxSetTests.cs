using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaVfxSetTests
    {
        [Test]
        public void DefaultVfxSet_AllRequiredPrefabsAssigned()
        {
            var vfxSet = Resources.Load<CombatArenaVfxSetSO>("DeadManZone/CombatArenaVfxSet");
            Assert.NotNull(vfxSet, "CombatArenaVfxSet.asset missing from Resources/DeadManZone/");

            Assert.NotNull(vfxSet.rifleMuzzle, "rifleMuzzle");
            Assert.NotNull(vfxSet.rifleImpact, "rifleImpact");
            Assert.NotNull(vfxSet.bulletTracer, "bulletTracer");
            Assert.NotNull(vfxSet.deathBurst, "deathBurst");
            Assert.NotNull(vfxSet.explosionSmall, "explosionSmall");
        }
    }
}
