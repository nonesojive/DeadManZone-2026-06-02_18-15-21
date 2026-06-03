using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class GameTagsTests
    {
        [Test]
        public void Combatant_Constant_IsStable()
        {
            Assert.AreEqual("Combatant", GameTags.Combatant);
            Assert.AreEqual("HQ", GameTags.Hq);
        }
    }
}
