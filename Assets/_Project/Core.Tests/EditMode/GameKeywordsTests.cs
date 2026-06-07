using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class GameKeywordsTests
    {
        [Test]
        public void Infantry_Constant_IsStable()
        {
            Assert.AreEqual("Infantry", GameKeywords.Infantry);
            Assert.AreEqual("Vehicle", GameKeywords.Vehicle);
            Assert.AreEqual("Echo", GameKeywords.Echo);
        }
    }
}
