using System.Collections.Generic;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CustomTagValidatorTests
    {
        [Test]
        public void TryValidate_RejectsBuiltInInfantryId()
        {
            var record = new CustomTagRecord
            {
                Id = GameTagIds.Infantry,
                DisplayName = "Infantry Copy",
                Category = TagCategory.Synergy,
                Tooltip = "Should fail."
            };

            Assert.IsFalse(CustomTagValidator.TryValidate(record, new List<CustomTagRecord>(), out string error));
            Assert.That(error, Does.Contain("reserved"));
        }

        [Test]
        public void TryValidate_RejectsInvalidIdFormat()
        {
            var record = new CustomTagRecord
            {
                Id = "Bad-Tag",
                DisplayName = "Bad",
                Category = TagCategory.Flavor,
                Tooltip = "Invalid id."
            };

            Assert.IsFalse(CustomTagValidator.TryValidate(record, new List<CustomTagRecord>(), out _));
        }

        [Test]
        public void TryValidate_AcceptsValidCustomTag()
        {
            var record = new CustomTagRecord
            {
                Id = "test_salvage",
                DisplayName = "Test Salvage",
                Category = TagCategory.Synergy,
                Tooltip = "Salvage test tag.",
                DisplayPriority = 25
            };

            Assert.IsTrue(CustomTagValidator.TryValidate(record, new List<CustomTagRecord>(), out string error), error);
        }

        [Test]
        public void TryValidate_RejectsDuplicateCustomIds()
        {
            var existing = new List<CustomTagRecord>
            {
                new()
                {
                    Id = "test_salvage",
                    DisplayName = "Existing",
                    Category = TagCategory.Synergy,
                    Tooltip = "Existing tag."
                }
            };

            var duplicate = new CustomTagRecord
            {
                Id = "test_salvage",
                DisplayName = "Duplicate",
                Category = TagCategory.Flavor,
                Tooltip = "Duplicate tag."
            };

            Assert.IsFalse(CustomTagValidator.TryValidate(duplicate, existing, out string error));
            Assert.That(error, Does.Contain("already exists"));
        }

        [Test]
        public void TryValidate_RejectsAttackTypeCategory()
        {
            var record = new CustomTagRecord
            {
                Id = "custom_ballistic",
                DisplayName = "Custom Ballistic",
                Category = TagCategory.AttackType,
                Tooltip = "Not allowed."
            };

            Assert.IsFalse(CustomTagValidator.TryValidate(record, new List<CustomTagRecord>(), out _));
        }
    }
}
