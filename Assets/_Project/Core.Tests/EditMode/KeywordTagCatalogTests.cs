using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class KeywordTagCatalogTests
    {
        [Test]
        public void Catalog_ContainsExpectedKeywordCounts()
        {
            Assert.AreEqual(17, KeywordTagCatalog.GetByCategory(TagCategory.Synergy).Count);
            Assert.AreEqual(13, KeywordTagCatalog.GetByCategory(TagCategory.Ability).Count);
            Assert.AreEqual(12, KeywordTagCatalog.GetByCategory(TagCategory.Flavor).Count);
        }

        [Test]
        public void Catalog_AllEntries_RegisteredInTagRegistry()
        {
            foreach (var entry in KeywordTagCatalog.All)
            {
                Assert.IsTrue(TagRegistry.TryGet(entry.Id, out var tag), $"Missing registry entry for '{entry.Id}'.");
                Assert.AreEqual(entry.Category, tag.Category);
                Assert.AreEqual(entry.DisplayName, tag.DisplayName);
            }
        }
    }
}
