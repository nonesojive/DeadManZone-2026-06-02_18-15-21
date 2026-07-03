using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BoardTerrainArtSOTests
    {
        [Test]
        public void PickTile_CombatFrontColumn_UsesFrontPool()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.combatBoardTiles = new[] { MakeSprite("combat") };
            art.combatFrontColumnTiles = new[] { MakeSprite("front") };

            var front = art.PickTile(BoardKind.Combat, new GridCoord(5, 2), boardWidth: 6);
            var interior = art.PickTile(BoardKind.Combat, new GridCoord(4, 2), boardWidth: 6);

            Assert.That(front.name, Is.EqualTo("front"));
            Assert.That(interior.name, Is.EqualTo("combat"));
        }

        [Test]
        public void PickTile_HqBoard_UsesHqPool()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.combatBoardTiles = new[] { MakeSprite("combat") };
            art.hqBoardTiles = new[] { MakeSprite("hq") };

            var tile = art.PickTile(BoardKind.Hq, new GridCoord(1, 3), boardWidth: 3);
            Assert.That(tile.name, Is.EqualTo("hq"));
        }

        [Test]
        public void PickTile_FrontPoolEmpty_FallsBackToCombatPool()
        {
            var art = ScriptableObject.CreateInstance<BoardTerrainArtSO>();
            art.combatBoardTiles = new[] { MakeSprite("combat") };
            art.combatFrontColumnTiles = System.Array.Empty<Sprite>();

            var front = art.PickTile(BoardKind.Combat, new GridCoord(5, 0), boardWidth: 6);
            Assert.That(front.name, Is.EqualTo("combat"));
        }

        private static Sprite MakeSprite(string name)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.name = name;
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
