using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class RngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new Rng(12345);
            var b = new Rng(12345);
            Assert.AreEqual(a.NextInt(0, 100), b.NextInt(0, 100));
            Assert.AreEqual(a.NextInt(0, 100), b.NextInt(0, 100));
        }

        [Test]
        public void NextInt_RespectsBounds()
        {
            var rng = new Rng(1);
            for (int i = 0; i < 100; i++)
            {
                int v = rng.NextInt(2, 5);
                Assert.That(v, Is.InRange(2, 4));
            }
        }
    }
}
