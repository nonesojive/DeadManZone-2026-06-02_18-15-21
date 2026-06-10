using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatGridMapperTests
    {
        [Test]
        public void ToWorld_CentersBattlefieldOnX()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var mapper = new CombatGridMapper(layout, cellWidth: 2f, cellDepth: 2f);

            var center = new GridCoord(layout.TotalWidth / 2, 0);
            Vector3 world = mapper.ToWorld(center);

            Assert.AreEqual(0f, world.x, 0.001f);
        }

        [Test]
        public void ToWorld_FrontRow_HasLowerZThanBackRow()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var mapper = new CombatGridMapper(layout, 2f, 2f);

            var backRow = mapper.ToWorld(new GridCoord(0, 0));
            var frontRow = mapper.ToWorld(new GridCoord(0, layout.Height - 1));

            Assert.Greater(backRow.z, frontRow.z);
        }

        [Test]
        public void TryWorldToCoord_RoundTrips()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var mapper = new CombatGridMapper(layout, 1.8f, 1.8f);
            var original = new GridCoord(3, 2);

            Vector3 world = mapper.ToWorld(original);
            Assert.IsTrue(mapper.TryWorldToCoord(world, out var roundTripped));
            Assert.AreEqual(original, roundTripped);
        }
    }
}
