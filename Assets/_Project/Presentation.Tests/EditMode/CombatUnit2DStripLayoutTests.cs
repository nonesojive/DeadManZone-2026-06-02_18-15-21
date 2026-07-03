using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatUnit2DStripLayoutTests
    {
        [Test]
        public void DetectFromTexture_FullSquareGrid_UsesAllCells()
        {
            var texture = CreateGridTexture(columns: 4, rows: 4, filledCells: 16);
            Assert.IsTrue(CombatUnit2DStripLayout.TryDetectFromTexture(texture, 32, out int columns, out int frameCount));
            Assert.AreEqual(4, columns);
            Assert.AreEqual(16, frameCount);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void DetectFromTexture_PartialRowMajorGrid_StopsAtFirstEmptyCell()
        {
            var texture = CreateGridTexture(columns: 4, rows: 4, filledCells: 10);
            Assert.IsTrue(CombatUnit2DStripLayout.TryDetectFromTexture(texture, 32, out int columns, out int frameCount));
            Assert.AreEqual(4, columns);
            Assert.AreEqual(10, frameCount);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void DetectBestFromTexture_Prefers512PxCellsOn4096AutospriteSheet()
        {
            // Simulates Autosprite 4096² @ 512px (8×8) vs false 256px quarter-frames.
            var texture = CreateGridTexture(columns: 8, rows: 8, cell: 128, filledCells: 64);
            Assert.IsTrue(CombatUnit2DStripLayout.TryDetectBestFromTexture(
                texture, out int columns, out int frameCount, out int cellPixels));
            Assert.AreEqual(128, cellPixels);
            Assert.AreEqual(8, columns);
            Assert.AreEqual(64, frameCount);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void FramesPerSecondForDuration_DividesFrameCountByTargetSeconds()
        {
            Assert.AreEqual(64f, CombatUnit2DStripLayout.FramesPerSecondForDuration(256, 4f), 0.001f);
            Assert.AreEqual(24f, CombatUnit2DStripLayout.FramesPerSecondForDuration(0, 4f), 0.001f);
        }

        private static Texture2D CreateGridTexture(int columns, int rows, int filledCells, int cell = 32)
        {
            var texture = new Texture2D(columns * cell, rows * cell, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var clear = new Color[columns * rows * cell * cell];
            texture.SetPixels(clear);

            for (int i = 0; i < filledCells; i++)
            {
                int col = i % columns;
                int row = i / columns;
                int x0 = col * cell;
                int y0 = texture.height - (row + 1) * cell;
                texture.SetPixel(x0, y0, Color.white);
            }

            texture.Apply();
            return texture;
        }
    }
}
