namespace DeadManZone.Core.Common
{
    public readonly struct GridCoord
    {
        public int X { get; }
        public int Y { get; }

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) =>
            obj is GridCoord other && X == other.X && Y == other.Y;

        public override int GetHashCode() => (X * 397) ^ Y;

        public override string ToString() => $"({X},{Y})";
    }
}
