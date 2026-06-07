using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TagRegistryTests
    {
        [Test]
        public void Registry_ContainsInfantryPrimaryTag()
        {
            var tag = TagRegistry.Get(GameTagIds.Infantry);
            Assert.AreEqual(TagCategory.Primary, tag.Category);
            Assert.IsTrue(tag.PlayerVisible);
            Assert.AreEqual("Infantry", tag.DisplayName);
        }

        [Test]
        public void Registry_UnknownId_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => TagRegistry.Get("not_a_real_tag"));
        }
    }
}
