using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class AttackTypeProfileCatalogTests
    {
        [Test]
        public void Catalog_ContainsSevenAttackTypes()
        {
            Assert.AreEqual(7, AttackTypeProfileCatalog.All.Count);
        }

        [Test]
        public void ToTagId_MapsBallisticEnum()
        {
            Assert.AreEqual("ballistic", AttackTypeTags.ToTagId(AttackType.Ballistic));
        }

        [Test]
        public void Ballistic_Tooltip_MentionsMediumAndHeavy()
        {
            var profile = AttackTypeProfileCatalog.Get(AttackType.Ballistic);
            Assert.That(profile.Tooltip, Does.Contain("Medium"));
            Assert.That(profile.Tooltip, Does.Contain("Heavy"));
        }

        [Test]
        public void Enum_IncludesNewValues()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Shredding));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Fire));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Melee));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AttackType), AttackType.Gas));
        }
    }
}
