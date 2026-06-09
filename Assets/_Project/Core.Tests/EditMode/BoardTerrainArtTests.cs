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
        public void HasTerrainTiles_IsFalseWhenBattlefieldBackdropSet()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.frontTiles = new[] { CreateStubSprite("front") };
            art.battlefieldBackdrop = CreateStubSprite("backdrop");

            Assert.IsTrue(art.HasBattlefieldBackdrop);
            Assert.IsFalse(art.HasTerrainTiles);
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
