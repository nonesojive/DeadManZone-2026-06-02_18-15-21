using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatUnit2DStripPlayerTests
    {
        private Texture2D _texture;
        private Sprite _sheet;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(512, 128, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;
            for (int x = 0; x < _texture.width; x++)
            {
                for (int y = 0; y < _texture.height; y++)
                    _texture.SetPixel(x, y, new Color(x / 512f, 0f, 0f, 1f));
            }

            _texture.Apply();
            _sheet = Sprite.Create(_texture, new Rect(0, 0, 512, 128), new Vector2(0.5f, 0f), 128f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_sheet != null)
                Object.DestroyImmediate(_sheet);
            if (_texture != null)
                Object.DestroyImmediate(_texture);
        }

        [Test]
        public void ResolveFrameIndex_LoopsWithinFrameCount()
        {
            var strip = new CombatUnit2DStrip
            {
                frameCount = 4,
                framesPerSecond = 2f,
                loop = true
            };

            Assert.That(CombatUnit2DStripPlayer.ResolveFrameIndex(strip, 0f), Is.EqualTo(0));
            Assert.That(CombatUnit2DStripPlayer.ResolveFrameIndex(strip, 0.75f), Is.EqualTo(1));
            Assert.That(CombatUnit2DStripPlayer.ResolveFrameIndex(strip, 2f), Is.EqualTo(0));
        }

        [Test]
        public void ResolveFrameIndex_ClampsOneShotAtLastFrame()
        {
            var strip = new CombatUnit2DStrip
            {
                frameCount = 4,
                framesPerSecond = 2f,
                loop = false
            };

            Assert.That(CombatUnit2DStripPlayer.ResolveFrameIndex(strip, 99f), Is.EqualTo(3));
        }

        [Test]
        public void Tick_OneShotReturnsToIdle()
        {
            var set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
            set.idle = new CombatUnit2DStrip { sheet = _sheet, frameCount = 4, framesPerSecond = 4f, loop = true };
            set.shoot = new CombatUnit2DStrip { sheet = _sheet, frameCount = 4, framesPerSecond = 4f, loop = false };

            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);
            player.Play(CombatUnit2DAnimState.Shoot);
            player.Tick(1.5f);

            Assert.That(player.State, Is.EqualTo(CombatUnit2DAnimState.Idle));
            Object.DestroyImmediate(set);
        }

        [Test]
        public void SliceUnitStrip_GridIsRowMajorFromTopLeft()
        {
            // 2x2 grid in a 256x256 sheet; texture space is bottom-left origin,
            // so frame 0 (top-left) must map to the upper rect (y = 128).
            var gridTex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            gridTex.Apply();
            var gridSheet = Sprite.Create(gridTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0f), 128f);

            var strip = new CombatUnit2DStrip
            {
                sheet = gridSheet,
                frameCount = 4,
                columns = 2,
                framesPerSecond = 4f,
                loop = true
            };

            var frames = CombatUnit2DStripPlayer.SliceUnitStrip(strip);
            Assert.That(frames.Length, Is.EqualTo(4));
            Assert.That(frames[0].rect, Is.EqualTo(new Rect(0, 128, 128, 128)));   // top-left
            Assert.That(frames[1].rect, Is.EqualTo(new Rect(128, 128, 128, 128))); // top-right
            Assert.That(frames[2].rect, Is.EqualTo(new Rect(0, 0, 128, 128)));     // bottom-left
            Assert.That(frames[3].rect, Is.EqualTo(new Rect(128, 0, 128, 128)));   // bottom-right

            foreach (var f in frames)
                Object.DestroyImmediate(f);
            Object.DestroyImmediate(gridSheet);
            Object.DestroyImmediate(gridTex);
        }

        [Test]
        public void ResolveSprite_UsesActiveStripFrame()
        {
            var set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
            set.walk = new CombatUnit2DStrip { sheet = _sheet, frameCount = 4, framesPerSecond = 4f, loop = true };

            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);
            player.Play(CombatUnit2DAnimState.Walk);

            var sprite = player.ResolveSprite(null);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.width, Is.EqualTo(128f).Within(0.01f));
            Object.DestroyImmediate(set);
        }
    }
}
