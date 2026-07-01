using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class PieceAbilityCardDescriptionTests
    {
        [Test]
        public void BuildAbilityLines_FieldHospital_UsesGeneratedFightStartText()
        {
            var piece = TestPieces.With(
                TestPieces.RifleSquad(),
                abilities: new[]
                {
                    new PieceAbilityDefinition
                    {
                        Id = "field_hospital_infantry_hp",
                        Trigger = PieceAbilityTrigger.FightStart,
                        NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                        Stat = SynergyStat.MaxHp,
                        ModType = SynergyModType.Flat,
                        Magnitude = 10
                    }
                });

            var lines = PieceCardTooltipFormatter.BuildAbilityLines(piece);

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Does.Contain("At fight start"));
            Assert.That(lines[0], Does.Contain("+10 max HP"));
        }

        [Test]
        public void BuildAbilityLines_SupplyDepot_IncludesPassiveIncomeLine()
        {
            var piece = new PieceDefinition
            {
                Id = "supply_depot",
                DisplayName = "Supply Depot"
            };

            var lines = PieceCardTooltipFormatter.BuildAbilityLines(piece);

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Does.Contain("+5 supplies"));
        }

        [Test]
        public void BuildAbilityLines_RecruitmentOffice_IncludesMusterLine()
        {
            var piece = new PieceDefinition
            {
                Id = "recruitment_office",
                DisplayName = "Recruitment Office",
                MusterPerShop = 1
            };

            var lines = PieceCardTooltipFormatter.BuildAbilityLines(piece);

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Does.Contain("+1 manpower"));
        }

        [Test]
        public void BuildAbilityLines_PrefersAuthoredCardDescription()
        {
            var piece = TestPieces.With(
                TestPieces.RifleSquad(),
                abilities: new[]
                {
                    new PieceAbilityDefinition
                    {
                        Id = "custom",
                        CardDescription = "Custom authored line.",
                        Trigger = PieceAbilityTrigger.FightStart,
                        Stat = SynergyStat.Damage,
                        ModType = SynergyModType.Flat,
                        Magnitude = 1
                    }
                });

            var lines = PieceCardTooltipFormatter.BuildAbilityLines(piece);

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.AreEqual("Custom authored line.", lines[0]);
        }
    }
}
