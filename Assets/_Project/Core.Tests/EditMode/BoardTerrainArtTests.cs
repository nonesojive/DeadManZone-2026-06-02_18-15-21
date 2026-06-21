using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BoardTerrainArtTests
    {
        [Test]
        public void PickTile_IsStableForSameCoordinate()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.frontTiles = new[] { CreateStubSprite("a"), CreateStubSprite("b") };

            var coord = new GridCoord(3, 7);
            var first = art.PickTile(ZoneType.Front, coord);
            var second = art.PickTile(ZoneType.Front, coord);

            Assert.AreSame(first, second);
            Object.DestroyImmediate(art);
        }

        [Test]
        public void PickTile_ReturnsNullWhenPoolEmpty()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            Assert.IsNull(art.PickTile(ZoneType.Rear, new GridCoord(0, 0)));
            Object.DestroyImmediate(art);
        }

        [Test]
        public void PickTile_ReturnsCellSpriteForAllZonesWhenSet()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.cellSprite = CreateStubSprite("support-b1");
            art.rearTiles = new[] { CreateStubSprite("rear") };
            art.frontTiles = new[] { CreateStubSprite("front") };

            var coord = new GridCoord(2, 4);
            Assert.AreSame(art.cellSprite, art.PickTile(ZoneType.Rear, coord));
            Assert.AreSame(art.cellSprite, art.PickTile(ZoneType.Support, coord));
            Assert.AreSame(art.cellSprite, art.PickTile(ZoneType.Front, coord));
            Assert.IsTrue(art.HasTerrainTiles);
            Object.DestroyImmediate(art);
        }

        [Test]
        public void HasTerrainTiles_IsFalseWhenBattlefieldBackdropSet()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.frontTiles = new[] { CreateStubSprite("front") };
            art.battlefieldBackdrop = CreateStubSprite("backdrop");

            Assert.IsTrue(art.HasBattlefieldBackdrop);
            Assert.IsFalse(art.HasTerrainTiles);
            Object.DestroyImmediate(art);
        }

        [Test]
        public void PickReserveSlot_IsStableForSameCoordinate()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.reserveSlotTiles = new[] { CreateStubSprite("g1"), CreateStubSprite("g2") };

            var coord = new GridCoord(5, 1);
            var first = art.PickReserveSlot(coord);
            var second = art.PickReserveSlot(coord);

            Assert.AreSame(first, second);
            Object.DestroyImmediate(art);
        }

        [Test]
        public void PickReserveSlot_ReturnsNullWhenPoolEmpty()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            Assert.IsNull(art.PickReserveSlot(new GridCoord(0, 0)));
            Object.DestroyImmediate(art);
        }

        private static Sprite CreateStubSprite(string name)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            return sprite;
        }
    }
}
