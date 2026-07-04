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
        public void DetectBestFromTexture_PrefersLargestDividingCellSize()
        {
            // 1024² sheet: 512 divides it, so detection must pick 512 cells (2×2 grid),
            // never a smaller candidate that would slice frames into quarters.
            var texture = CreateGridTexture(columns: 2, rows: 2, cell: 512, filledCells: 4);
            Assert.IsTrue(CombatUnit2DStripLayout.TryDetectBestFromTexture(
                texture, out int columns, out int frameCount, out int cellPixels));
            Assert.AreEqual(512, cellPixels);
            Assert.AreEqual(2, columns);
            Assert.AreEqual(4, frameCount);
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
