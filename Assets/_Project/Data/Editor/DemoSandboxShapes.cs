using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class DemoSandboxShapes
    {
        public static readonly Vector2Int[] Single = { Vector2Int.zero };

        public static readonly Vector2Int[] VerticalPair =
        {
            Vector2Int.zero,
            new Vector2Int(0, 1)
        };

        public static readonly Vector2Int[] HorizontalPair =
        {
            Vector2Int.zero,
            Vector2Int.right
        };

        public static readonly Vector2Int[] TransportL =
        {
            Vector2Int.zero,
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        public static readonly Vector2Int[] SiegePlate =
        {
            Vector2Int.zero,
            Vector2Int.right,
            new Vector2Int(2, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(2, 1)
        };

        public static readonly Vector2Int[] Square2x2 =
        {
            Vector2Int.zero,
            Vector2Int.right,
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        public static readonly Vector2Int[] Walker =
        {
            Vector2Int.zero,
            Vector2Int.right,
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };
    }
}
