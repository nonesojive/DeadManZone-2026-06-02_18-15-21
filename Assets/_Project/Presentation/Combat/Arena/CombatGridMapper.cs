using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatGridMapper
    {
        private readonly BattlefieldLayout _layout;
        private readonly float _cellWidth;
        private readonly float _cellDepth;

        public CombatGridMapper(BattlefieldLayout layout, float cellWidth, float cellDepth)
        {
            _layout = layout;
            _cellWidth = cellWidth;
            _cellDepth = cellDepth;
        }

        public Vector3 ToWorld(GridCoord coord)
        {
            float x = (coord.X + 0.5f - _layout.TotalWidth * 0.5f) * _cellWidth;
            float z = (_layout.Height * 0.5f - coord.Y - 0.5f) * _cellDepth;
            return new Vector3(x, 0f, z);
        }

        public bool TryWorldToCoord(Vector3 world, out GridCoord coord)
        {
            float xIndex = world.x / _cellWidth + _layout.TotalWidth * 0.5f - 0.5f;
            float yIndex = _layout.Height * 0.5f - world.z / _cellDepth - 0.5f;

            int x = Mathf.RoundToInt(xIndex);
            int y = Mathf.RoundToInt(yIndex);
            if (x < 0 || y < 0 || x >= _layout.TotalWidth || y >= _layout.Height)
            {
                coord = default;
                return false;
            }

            coord = new GridCoord(x, y);
            return true;
        }
    }
}
