using DeadManZone.Core.Board;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatStatEnumsTests
    {
        [Test]
        public void GrantedAbility_IncludesDemoAbilities()
        {
            Assert.AreEqual(0, (int)GrantedAbility.None);
            Assert.AreEqual(1, (int)GrantedAbility.GrenadeLob);
            Assert.AreEqual(2, (int)GrantedAbility.ShieldAllies);
            Assert.AreEqual(3, (int)GrantedAbility.CannonBlast);
        }
    }
}
