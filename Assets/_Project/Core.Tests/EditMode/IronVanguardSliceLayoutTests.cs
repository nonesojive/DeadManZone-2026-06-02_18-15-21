using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class IronVanguardSliceLayoutTests
    {
        [Test]
        public void IronVanguardSkirmish_BuildsBattlefieldWithExpectedCombatants()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database, "Run DeadManZone/Generate Vertical Slice Content first.");

            var battlefield = CombatSliceLayouts.BuildIronVanguardSkirmish(database);
            Assert.NotNull(battlefield);

            int combatants = 0;
            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null)
                    continue;
                if (PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    combatants++;
            }

            // 2 player rifles + 1 tank + 2 enemy rifles (+ field gun if combatant-tagged)
            Assert.GreaterOrEqual(combatants, 5,
                "Slice should field at least five combatant-tagged units.");
        }

        [Test]
        public void IronVanguardSkirmish_AllPlacementsSucceed()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            Assert.DoesNotThrow(() => CombatSliceLayouts.BuildIronVanguardSkirmish(database));
        }
    }
}
