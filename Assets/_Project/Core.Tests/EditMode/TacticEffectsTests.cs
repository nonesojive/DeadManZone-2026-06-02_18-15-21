using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TacticEffectsTests
    {
        [Test]
        public void Advance_IncreasesMovementMultiplier()
        {
            Assert.Greater(TacticEffects.GetMovementChargeMultiplier(TacticType.Advance), 100);
        }

        [Test]
        public void StandGround_DecreasesMovementMultiplier()
        {
            Assert.Less(TacticEffects.GetMovementChargeMultiplier(TacticType.StandGround), 100);
        }

        [Test]
        public void DisciplinedFire_GrantsDamageBuff()
        {
            Assert.Greater(TacticEffects.GetDamageBuff(TacticType.DisciplinedFire), 0);
        }
    }
}
