namespace DeadManZone.Core.Common
{
    /// <summary>
    /// Named, order-independent RNG sub-streams for seeded runs. Every system that rolls
    /// dice derives its own seed as hash(runSeed, streamName, index) instead of consuming
    /// from a shared stream or hand-rolled arithmetic (the old `RunSeed + FightIndex*1000`
    /// style collided across systems and shifted whenever a new consumer was inserted).
    /// Adding a consumer can never perturb another stream's rolls — the invariant that
    /// makes "same seed + same choices = same run" survive new features.
    /// FNV-1a 32-bit: stable across platforms and releases; golden-value tested.
    /// </summary>
    public static class SeedStreams
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        /// <summary>Deterministic seed for one (stream, index, subIndex) cell of the run.</summary>
        public static int Derive(int runSeed, string streamName, int index = 0, int subIndex = 0)
        {
            uint hash = FnvOffsetBasis;
            hash = HashInt(hash, runSeed);
            if (streamName != null)
            {
                for (int i = 0; i < streamName.Length; i++)
                {
                    hash ^= streamName[i];
                    hash *= FnvPrime;
                }
            }

            hash = HashInt(hash, index);
            hash = HashInt(hash, subIndex);
            return unchecked((int)hash);
        }

        /// <summary>A fresh deterministic Rng positioned at the stream cell.</summary>
        public static Rng Stream(int runSeed, string streamName, int index = 0, int subIndex = 0) =>
            new Rng(Derive(runSeed, streamName, index, subIndex));

        private static uint HashInt(uint hash, int value)
        {
            unchecked
            {
                uint v = (uint)value;
                hash ^= v & 0xFF;
                hash *= FnvPrime;
                hash ^= (v >> 8) & 0xFF;
                hash *= FnvPrime;
                hash ^= (v >> 16) & 0xFF;
                hash *= FnvPrime;
                hash ^= (v >> 24) & 0xFF;
                hash *= FnvPrime;
                return hash;
            }
        }
    }
}
