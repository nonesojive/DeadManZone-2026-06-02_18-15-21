using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>SeedStreams is the seeded-run foundation: named, order-independent
    /// sub-streams. The golden-value test LOCKS the hash — if it ever fails, existing
    /// mid-run saves and shared seeds break; bump the save schema before changing it.</summary>
    public sealed class SeedStreamsTests
    {
        [Test]
        public void Derive_IsStable_GoldenValues()
        {
            // Golden values recorded 2026-07-12 (FNV-1a 32, independently computed).
            // Do NOT update casually — a change here invalidates every shared seed
            // and in-flight save; bump the save schema if the hash must change.
            Assert.AreEqual(463513895, SeedStreams.Derive(20260712, "combat", 1));
            Assert.AreEqual(-2017634889, SeedStreams.Derive(20260712, "shop", 3, 2));
        }

        [Test]
        public void Derive_DiffersAcrossStreamNames_SameSeedAndIndex()
        {
            Assert.AreNotEqual(
                SeedStreams.Derive(42, "combat", 5),
                SeedStreams.Derive(42, "shop", 5),
                "two systems on the same round must not share rolls");
        }

        [Test]
        public void Derive_DiffersAcrossIndices_AndSubIndices()
        {
            Assert.AreNotEqual(SeedStreams.Derive(42, "shop", 1), SeedStreams.Derive(42, "shop", 2));
            Assert.AreNotEqual(SeedStreams.Derive(42, "shop", 1, 0), SeedStreams.Derive(42, "shop", 1, 1));
        }

        [Test]
        public void Derive_DiffersAcrossRunSeeds()
        {
            Assert.AreNotEqual(SeedStreams.Derive(1, "combat", 1), SeedStreams.Derive(2, "combat", 1));
        }

        [Test]
        public void Stream_YieldsUsableRng_EvenIfDeriveHitsZero()
        {
            // Rng(0) guards to 1 internally; a stream must never produce a stuck generator.
            var rng = SeedStreams.Stream(0, "anything", 0);
            int roll = rng.NextInt(0, 100);
            Assert.That(roll, Is.InRange(0, 99));
        }

        [Test]
        public void Derive_NoCollisionsAcrossRealisticRunSpace()
        {
            // All (stream, round, reroll) cells one long run touches must be distinct.
            var seen = new System.Collections.Generic.HashSet<int>();
            string[] streams = { "combat", "shop", "options", "themes", "bosses", "pity" };
            foreach (var stream in streams)
                for (int round = 0; round < 30; round++)
                    for (int sub = 0; sub < 6; sub++)
                        Assert.IsTrue(
                            seen.Add(SeedStreams.Derive(20260712, stream, round, sub)),
                            $"collision at ({stream}, {round}, {sub})");
        }
    }
}
