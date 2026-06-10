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

        [Test]
        public void Registry_ContainsBallisticAttackTypeTag()
        {
            var tag = TagRegistry.Get(GameTagIds.Ballistic);
            Assert.AreEqual(TagCategory.AttackType, tag.Category);
            Assert.AreEqual("Ballistic", tag.DisplayName);
        }

        [Test]
        public void Registry_HasSeventeenSynergyTags()
        {
            Assert.AreEqual(17, TagRegistry.GetByCategory(TagCategory.Synergy).Count);
        }

        [Test]
        public void Registry_HasThirteenAbilityTags()
        {
            Assert.AreEqual(13, TagRegistry.GetByCategory(TagCategory.Ability).Count);
        }

        [Test]
        public void Registry_HasTwelveFlavorTags()
        {
            Assert.AreEqual(12, TagRegistry.GetByCategory(TagCategory.Flavor).Count);
        }

        [Test]
        public void Registry_HasSevenAttackTypeTags()
        {
            Assert.AreEqual(7, TagRegistry.GetByCategory(TagCategory.AttackType).Count);
        }

        [Test]
        public void Registry_MedicSynergyTag_HasSheetTooltip()
        {
            var tag = TagRegistry.Get(GameTagIds.Medic);
            Assert.AreEqual(TagCategory.Synergy, tag.Category);
            Assert.That(tag.Tooltip, Does.Contain("infantry"));
        }

        [Test]
        public void Registry_StealthAbilityTag_HasSheetTooltip()
        {
            var tag = TagRegistry.Get(GameTagIds.Stealth);
            Assert.AreEqual(TagCategory.Ability, tag.Category);
            Assert.That(tag.Tooltip, Does.Contain("hidden"));
        }

        [Test]
        public void Registry_FortifiedFlavorTag_HasSheetTooltip()
        {
            var tag = TagRegistry.Get(GameTagIds.Fortified);
            Assert.AreEqual(TagCategory.Flavor, tag.Category);
            Assert.That(tag.Tooltip, Does.Contain("armor"));
        }
    }
}
