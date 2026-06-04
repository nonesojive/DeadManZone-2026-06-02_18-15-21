using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TacticTypeTests
    {
        [Test]
        public void TacticType_HasFourDemoTactics()
        {
            Assert.AreEqual(4, System.Enum.GetNames(typeof(TacticType)).Length);
        }
    }
}
