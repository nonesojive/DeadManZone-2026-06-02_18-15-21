using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceAbilityDefinitionTests
    {
        [Test]
        public void Definition_StoresIdAndDescription()
        {
            var def = new PieceAbilityDefinition
            {
                Id = "adjacent_infantry_armor_plus_one",
                CardDescription = "Adjacent infantry gain +1 armor.",
                Trigger = PieceAbilityTrigger.AdjacentAura,
                NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                Stat = SynergyStat.ArmorType,
                ModType = SynergyModType.Flat,
                Magnitude = 1
            };

            Assert.AreEqual("adjacent_infantry_armor_plus_one", def.Id);
            Assert.AreEqual(PieceAbilityTrigger.AdjacentAura, def.Trigger);
            Assert.AreEqual(1, def.Magnitude);
        }
    }
}
