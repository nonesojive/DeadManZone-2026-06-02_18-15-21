using DeadManZone.Core.Board;
using DeadManZone.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class BoardViewLocatorTests
    {
        [Test]
        public void FindByBinding_ReturnsMatchingBoardView()
        {
            var root = new GameObject("BoardRoot");
            var combatGo = new GameObject("CombatBoard");
            combatGo.transform.SetParent(root.transform, false);
            var combat = combatGo.AddComponent<BoardView>();
            combat.SetBoardBinding(BoardKind.Combat);

            var hqGo = new GameObject("HqBoard");
            hqGo.transform.SetParent(root.transform, false);
            var hq = hqGo.AddComponent<BoardView>();
            hq.SetBoardBinding(BoardKind.Hq);

            try
            {
                Assert.AreSame(combat, BoardView.FindByBinding(BoardKind.Combat));
                Assert.AreSame(hq, BoardView.FindByBinding(BoardKind.Hq));
                Assert.AreSame(combat, BoardView.FindCombatBoard());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
