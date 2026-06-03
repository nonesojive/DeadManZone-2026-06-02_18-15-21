namespace DeadManZone.Core.Common
{
    /// <summary>Deterministic Xorshift32 — same seed, same sequence across platforms.</summary>
    public sealed class Rng
    {
        private uint _state;

        public Rng(int seed)
        {
            _state = seed == 0 ? 1u : (uint)seed;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new System.ArgumentOutOfRangeException(nameof(maxExclusive));

            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat()
        {
            return NextUInt() / (float)uint.MaxValue;
        }

        private uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }
    }
}
