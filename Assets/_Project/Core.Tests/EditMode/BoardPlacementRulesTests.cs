using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class BoardPlacementRulesTests
    {
        [Test]
        public void ResolveTargetBoard_BuildingGoesToHq()
        {
            var piece = TestPieces.CommandBunker();
            Assert.AreEqual(BoardKind.Hq, BoardPlacementRules.ResolveTargetBoard(piece));
        }

        [Test]
        public void ResolveTargetBoard_InfantryGoesToCombat()
        {
            Assert.AreEqual(BoardKind.Combat, BoardPlacementRules.ResolveTargetBoard(TestPieces.RifleSquad()));
        }

        [Test]
        public void ResolveTargetBoard_StructureGoesToCombat()
        {
            var structure = TestPieces.CreateUnit("nest", primary: GameTagIds.Structure);
            Assert.AreEqual(BoardKind.Combat, BoardPlacementRules.ResolveTargetBoard(structure));
        }
    }
}
