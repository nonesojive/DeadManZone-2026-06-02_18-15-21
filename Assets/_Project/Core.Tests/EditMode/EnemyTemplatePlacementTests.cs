using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class EnemyTemplatePlacementTests
    {
        [Test]
        public void AllEnemyTemplates_BuildOnIronVanguardLayout()
        {
            var database = ContentDatabase.Load();
            if (database == null || database.Pieces.Count == 0)
            {
                Assert.Ignore("ContentDatabase not found.");
            }

            var faction = database.GetFaction("iron_vanguard");
            Assert.NotNull(faction);
            var registry = database.BuildRegistry();

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                var template = database.GetEnemyTemplate(fight);
                Assert.NotNull(template, $"Missing enemy template for fight {fight}.");

                try
                {
                    template.BuildBoard(faction, registry);
                }
                catch (System.Exception ex)
                {
                    Assert.Fail($"Fight {fight} ({template.displayName}) has invalid piece placement: {ex.Message}");
                }
            }
        }
    }
}
