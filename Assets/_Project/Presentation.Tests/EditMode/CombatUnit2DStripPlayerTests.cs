using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatUnit2DStripPlayerTests
    {
        [Test]
        public void SliceUnitStrip_DownscaledGpuTexture_UsesActualWidthNotSourceRect()
        {
            // Source art 4096² @ 8 cols; GPU texture downscaled to 2048² (common import cap).
            var texture = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[2048 * 2048]);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 4096f, 4096f), new Vector2(0.5f, 0.05f), 256f);
            var strip = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = 64,
                columns = 8,
                framesPerSecond = 16f,
                loop = true
            };

            var frames = CombatUnit2DStripPlayer.SliceUnitStrip(strip);
            Assert.AreEqual(64, frames.Length);
            for (int i = 0; i < frames.Length; i++)
                Assert.NotNull(frames[i], $"frame {i} should slice without exceeding texture bounds");

            Object.DestroyImmediate(texture);
        }
    }
}
