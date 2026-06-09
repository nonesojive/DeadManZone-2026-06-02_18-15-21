using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class UnitCreatorShapePresets
    {
        public static readonly (string Label, Vector2Int[] Cells)[] All =
        {
            ("1x1", new[] { Vector2Int.zero }),
            ("1x2", new[] { Vector2Int.zero, Vector2Int.up }),
            ("2x1", new[] { Vector2Int.zero, Vector2Int.right }),
            ("2x2", new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up, new(1, 1) }),
            ("L", new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up }),
            ("T", new[] { Vector2Int.zero, Vector2Int.left, Vector2Int.right })
        };

        public static HashSet<Vector2Int> ToCellSet(IEnumerable<Vector2Int> cells)
        {
            var set = new HashSet<Vector2Int>();
            if (cells == null)
                return set;

            foreach (var cell in cells)
                set.Add(cell);
            return set;
        }

        public static Vector2Int[] FromCellSet(HashSet<Vector2Int> cells)
        {
            if (cells == null || cells.Count == 0)
                return new[] { Vector2Int.zero };

            var list = new List<Vector2Int>(cells);
            list.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return list.ToArray();
        }
    }
}
