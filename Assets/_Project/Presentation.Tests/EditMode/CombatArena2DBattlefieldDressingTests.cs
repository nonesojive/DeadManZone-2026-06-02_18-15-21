using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArena2DBattlefieldDressingTests
    {
        [Test]
        public void CropToContent_TrimsGenerousRectToOpaquePixels()
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[128 * 128]); // fully transparent
            for (int y = 40; y < 60; y++)
                for (int x = 20; x < 70; x++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();

            var crop = CombatArena2DBattlefieldDressing.CropToContent(
                texture, new RectInt(0, 0, 128, 128));

            Assert.AreEqual(20, crop.x);
            Assert.AreEqual(40, crop.y);
            Assert.AreEqual(50, crop.width);
            Assert.AreEqual(20, crop.height);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CropToLargestComponent_DiscardsNeighborSlivers()
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[128 * 128]);
            // Main prop blob.
            for (int y = 40; y < 70; y++)
                for (int x = 30; x < 90; x++)
                    texture.SetPixel(x, y, Color.white);
            // Sliver of a neighboring tile clipped by the generous rect.
            for (int y = 0; y < 6; y++)
                for (int x = 120; x < 128; x++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();

            var crop = CombatArena2DBattlefieldDressing.CropToLargestComponent(
                texture, new RectInt(0, 0, 128, 128));

            Assert.AreEqual(new RectInt(30, 40, 60, 30), crop,
                "largest blob should win; the sliver must be excluded");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CropToContent_EmptyRect_ReturnsClampedInput()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[64 * 64]);
            texture.Apply();

            var crop = CombatArena2DBattlefieldDressing.CropToContent(
                texture, new RectInt(8, 8, 32, 32));

            Assert.AreEqual(new RectInt(8, 8, 32, 32), crop);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CropToContent_RectOutsideTexture_IsClampedSafely()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[32 * 32]);
            texture.Apply();

            var crop = CombatArena2DBattlefieldDressing.CropToContent(
                texture, new RectInt(20, 20, 64, 64));

            Assert.LessOrEqual(crop.xMax, 32);
            Assert.LessOrEqual(crop.yMax, 32);

            Object.DestroyImmediate(texture);
        }
    }
}
