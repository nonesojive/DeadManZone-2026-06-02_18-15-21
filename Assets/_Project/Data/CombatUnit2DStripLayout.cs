using UnityEngine;

namespace DeadManZone.Data
{
    /// <summary>Infers grid strip layout from Autosprite-style square sprite sheets.</summary>
    public static class CombatUnit2DStripLayout
    {
        public const int DefaultCellPixels = 512;
        private static readonly int[] CellCandidates = { 512, 256, 128 };
        // GetPixel returns 0-1 floats; threshold must live in that range (10/255 ≈ 0.039).
        private const float AlphaThreshold01 = 10f / 255f;

        /// <summary>Picks the largest cell size that divides the sheet (512 → 256 → 128).
        /// Autosprite 4096² exports are 8×8 @ 512px; slicing at 256px shows a quarter-frame.</summary>
        public static bool TryDetectBestFromTexture(
            Texture2D texture,
            out int columns,
            out int frameCount,
            out int cellPixels)
        {
            columns = 0;
            frameCount = 0;
            cellPixels = 0;

            foreach (int candidate in CellCandidates)
            {
                if (!TryDetectFromTexture(texture, candidate, out columns, out frameCount))
                    continue;

                cellPixels = candidate;
                return true;
            }

            return false;
        }

        public static bool TryDetectFromTexture(
            Texture2D texture,
            int cellPixels,
            out int columns,
            out int frameCount)
        {
            columns = 0;
            frameCount = 0;
            if (texture == null || cellPixels < 1)
                return false;

            if (texture.width % cellPixels != 0 || texture.height % cellPixels != 0)
                return false;

            columns = texture.width / cellPixels;
            int rows = texture.height / cellPixels;
            int gridCells = columns * rows;
            if (gridCells < 1)
                return false;

            if (!texture.isReadable)
            {
                frameCount = gridCells;
                return true;
            }

            frameCount = CountSequentialFrames(texture, cellPixels, columns, rows);
            return frameCount > 0;
        }

        public static float FramesPerSecondForDuration(int frameCount, float targetDurationSeconds, float fallbackFps = 24f)
        {
            if (frameCount < 1 || targetDurationSeconds <= 0f)
                return fallbackFps;

            return frameCount / targetDurationSeconds;
        }

        internal static int CountSequentialFrames(Texture2D texture, int cellPixels, int columns, int rows)
        {
            int count = 0;
            int gridCells = columns * rows;
            for (int i = 0; i < gridCells; i++)
            {
                if (!CellHasContent(texture, cellPixels, columns, i))
                    break;

                count++;
            }

            return count;
        }

        private static bool CellHasContent(Texture2D texture, int cellPixels, int columns, int index)
        {
            int col = index % columns;
            int row = index / columns;
            int x0 = col * cellPixels;
            // Texture pixel origin is bottom-left; row 0 in our sheets is the top row.
            int y0 = texture.height - (row + 1) * cellPixels;

            for (int y = y0; y < y0 + cellPixels; y++)
            {
                for (int x = x0; x < x0 + cellPixels; x++)
                {
                    if (texture.GetPixel(x, y).a > AlphaThreshold01)
                        return true;
                }
            }

            return false;
        }
    }
}
