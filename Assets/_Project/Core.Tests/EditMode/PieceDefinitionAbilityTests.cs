using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceDefinitionAbilityTests
    {
        [Test]
        public void PieceDefinition_ExposesAbilitiesList()
        {
            var abilities = new[]
            {
                new PieceAbilityDefinition
                {
                    Id = "inspiring_move",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    Stat = SynergyStat.MoveChargePercent,
                    ModType = SynergyModType.Flat,
                    Magnitude = 5
                }
            };
            var piece = TestPieces.With(TestPieces.RifleSquad(), abilities: abilities);
            Assert.AreEqual(1, piece.Abilities.Count);
            Assert.AreEqual("inspiring_move", piece.Abilities[0].Id);
        }
    }
}
