using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TutorialEconomyTests
    {
        [Test]
        public void IronVanguard_StartingSupplies_Is125()
        {
            var database = ContentDatabase.Load();
            var faction = database.GetFaction("iron_vanguard");
            Assert.NotNull(faction);
            Assert.AreEqual(125, faction.startingSupplies);
        }
    }
}
