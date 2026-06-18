using DeadManZone.Core.Board;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class TopTroopsBattlefieldBuilderTests
    {
        [Test]
        public void Build_CreatesOneCellPerGridCoord()
        {
            var layout = new BattlefieldLayout(3, 2, 3, 4);
            var root = new GameObject("TestArena");

            try
            {
                var view = TopTroopsBattlefieldBuilder.Build(
                    root.transform,
                    layout,
                    1.8f,
                    1.8f,
                    TopTroopsBattlefieldPalette.FromConfig(null));

                Assert.NotNull(view);
                Assert.AreEqual(layout.TotalWidth * layout.Height, view.CellCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResolveCellColor_PlayerHalf_UsesPlayerZoneTint()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var palette = TopTroopsBattlefieldPalette.FromConfig(null);

            const int y = 0;
            Color player = TopTroopsBattlefieldBuilder.ResolveCellColor(layout, 0, y, palette);
            Color neutral = TopTroopsBattlefieldBuilder.ResolveCellColor(layout, layout.NeutralStartX, y, palette);
            Color enemy = TopTroopsBattlefieldBuilder.ResolveCellColor(layout, layout.EnemyOriginX, y, palette);

            Assert.That(player, Is.EqualTo(TopTroopsBattlefieldBuilder.ApplyCheckerShade(palette.PlayerZoneColor, 0, y)));
            Assert.That(neutral, Is.EqualTo(TopTroopsBattlefieldBuilder.ApplyCheckerShade(palette.NeutralZoneColor, layout.NeutralStartX, y)));
            Assert.That(enemy, Is.EqualTo(TopTroopsBattlefieldBuilder.ApplyCheckerShade(palette.EnemyZoneColor, layout.EnemyOriginX, y)));
        }

        [Test]
        public void Build_PositionsCellsUsingCombatGridMapper()
        {
            var layout = new BattlefieldLayout(2, 1, 2, 2);
            var root = new GameObject("TestArena");
            var mapper = new CombatGridMapper(layout, 1f, 1f);

            try
            {
                var view = TopTroopsBattlefieldBuilder.Build(
                    root.transform,
                    layout,
                    1f,
                    1f,
                    TopTroopsBattlefieldPalette.FromConfig(null));

                var cell = view.GetCell(0, 0);
                Assert.NotNull(cell);
                Vector3 expected = mapper.ToWorld(new DeadManZone.Core.Common.GridCoord(0, 0));
                Assert.That(cell.transform.position.x, Is.EqualTo(expected.x).Within(0.01f));
                Assert.That(cell.transform.position.z, Is.EqualTo(expected.z).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
