using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatUnit2DStripPlayerTests
    {
        [Test]
        public void SliceUnitStrip_GridSheet_SlicesAllFramesWithinTextureBounds()
        {
            // combatvisualv2 sheets: square grid, row-major from top-left (7×7 @ 49 frames).
            // Unity 6 rejects sprite rects larger than the texture, so the downscaled-GPU
            // case can only occur via the importer; slicing from texture.width/height keeps
            // both paths in bounds. This guards the in-bounds contract.
            var texture = new Texture2D(896, 896, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[896 * 896]);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 896f, 896f), new Vector2(0.5f, 0.05f), 256f);
            var strip = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = 49,
                columns = 7,
                framesPerSecond = 12f,
                loop = true
            };

            var frames = CombatUnit2DStripPlayer.SliceUnitStrip(strip);
            Assert.AreEqual(49, frames.Length);
            for (int i = 0; i < frames.Length; i++)
            {
                Assert.NotNull(frames[i], $"frame {i} should slice without exceeding texture bounds");
                Assert.Greater(frames[i].rect.width, 0f);
                Assert.Greater(frames[i].rect.height, 0f);
                Assert.LessOrEqual(frames[i].rect.width, 128f);
                Assert.LessOrEqual(frames[i].rect.height, 128f);
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SliceUnitStrip_GridSheet_CropsSharedContentAwayFromCellEdges()
        {
            var texture = new Texture2D(512, 256, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[512 * 256]);

            PaintRect(texture, 40, 60, 150, 130, Color.white);
            PaintRect(texture, 296, 60, 150, 130, Color.white);
            PaintRect(texture, 254, 80, 2, 60, Color.white);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 512f, 256f), new Vector2(0.5f, 0.05f), 256f);
            var strip = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = 2,
                columns = 2,
                framesPerSecond = 12f,
                loop = true
            };

            var frames = CombatUnit2DStripPlayer.SliceUnitStrip(strip);
            Assert.AreEqual(2, frames.Length);
            Assert.NotNull(frames[0]);
            Assert.NotNull(frames[1]);
            Assert.Less(frames[0].rect.xMax, 250f, "cell-edge bleed should be outside the shared crop");
            Assert.Greater(frames[1].rect.x, 256f, "second frame should use the same local crop inside its own cell");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ResolveSprite_SameSetAcrossPlayers_ReusesSharedFrameSprites()
        {
            var texture = new Texture2D(256, 128, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[256 * 128]);
            PaintRect(texture, 32, 24, 64, 80, Color.white);
            PaintRect(texture, 160, 24, 64, 80, Color.white);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 256f, 128f), new Vector2(0.5f, 0.05f), 256f);
            var set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
            set.idle = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = 2,
                columns = 2,
                framesPerSecond = 12f,
                loop = true
            };

            var first = new CombatUnit2DStripPlayer();
            var second = new CombatUnit2DStripPlayer();
            first.Bind(set);
            second.Bind(set);
            first.Play(CombatUnit2DAnimState.Idle);
            second.Play(CombatUnit2DAnimState.Idle);

            Assert.AreSame(first.ResolveSprite(null), second.ResolveSprite(null));

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        private static void PaintRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                    texture.SetPixel(px, py, color);
            }
        }
    }
}
