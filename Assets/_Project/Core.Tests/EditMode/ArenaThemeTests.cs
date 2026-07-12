using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>M4 Arena Theme keying: pool home sets, boss signature grounds, the
    /// seeded per-option roll, and legacy-save normalization.</summary>
    public sealed class ArenaThemeTests
    {
        // ---- keying ----

        [Test]
        public void HomeThemes_KnownPools_AreSubsetsOfTheCanonicalList()
        {
            foreach (var pool in new[] { "neutral", "crimson_legion", "ash_wraiths", "ironmarch_union" })
            {
                var home = ArenaThemes.HomeThemes(pool);
                Assert.IsNotEmpty(home, pool);
                CollectionAssert.IsSubsetOf(home.ToList(), ArenaThemes.All.ToList(), pool);
            }
        }

        [Test]
        public void HomeThemes_UnknownOrNullPool_FallsBackToDefault()
        {
            CollectionAssert.AreEqual(new[] { ArenaThemes.Default }, ArenaThemes.HomeThemes("no_such_pool").ToList());
            CollectionAssert.AreEqual(new[] { ArenaThemes.Default }, ArenaThemes.HomeThemes(null).ToList());
        }

        [Test]
        public void SignatureTheme_IsInsideThePoolsHomeSet()
        {
            foreach (var pool in new[] { "neutral", "crimson_legion", "ash_wraiths" })
                CollectionAssert.Contains(
                    ArenaThemes.HomeThemes(pool).ToList(), ArenaThemes.SignatureTheme(pool), pool);
        }

        [Test]
        public void SignatureTheme_PerPool_MatchesTheAuthoredKeying()
        {
            Assert.AreEqual(ArenaThemes.Trenchline, ArenaThemes.SignatureTheme("neutral"));
            Assert.AreEqual(ArenaThemes.RavagedTown, ArenaThemes.SignatureTheme("crimson_legion"));
            Assert.AreEqual(ArenaThemes.FogField, ArenaThemes.SignatureTheme("ash_wraiths"));
            Assert.AreEqual(ArenaThemes.Default, ArenaThemes.SignatureTheme("no_such_pool"));
        }

        // ---- seeded roll ----

        [Test]
        public void Roll_SameInputs_IsDeterministic()
        {
            for (int slot = 0; slot < 3; slot++)
                Assert.AreEqual(
                    ArenaThemes.Roll(4242, 3, slot, "crimson_legion"),
                    ArenaThemes.Roll(4242, 3, slot, "crimson_legion"),
                    $"slot {slot}");
        }

        [Test]
        public void Roll_AlwaysLandsInThePoolsHomeSet()
        {
            foreach (var pool in new[] { "neutral", "crimson_legion", "ash_wraiths" })
            for (int round = 1; round <= 8; round++)
            for (int slot = 0; slot < 3; slot++)
                CollectionAssert.Contains(
                    ArenaThemes.HomeThemes(pool).ToList(),
                    ArenaThemes.Roll(9001, round, slot, pool),
                    $"{pool} round {round} slot {slot}");
        }

        [Test]
        public void Roll_VariesAcrossRoundsOrSlots()
        {
            // 24 cells over a 3-theme set: identical output everywhere would mean the
            // stream cell (roundIndex, slot) isn't actually being consumed.
            var rolls = new List<string>();
            for (int round = 1; round <= 8; round++)
            for (int slot = 0; slot < 3; slot++)
                rolls.Add(ArenaThemes.Roll(777, round, slot, "crimson_legion"));
            Assert.Greater(rolls.Distinct().Count(), 1);
        }

        // ---- normalization (legacy saves) ----

        [Test]
        public void Normalize_NullEmptyOrUnknown_MapsToDefault()
        {
            Assert.AreEqual(ArenaThemes.Default, ArenaThemes.Normalize(null));
            Assert.AreEqual(ArenaThemes.Default, ArenaThemes.Normalize(""));
            Assert.AreEqual(ArenaThemes.Default, ArenaThemes.Normalize("volcano_lair"));
        }

        [Test]
        public void Normalize_CanonicalIds_PassThrough()
        {
            foreach (var id in ArenaThemes.All)
                Assert.AreEqual(id, ArenaThemes.Normalize(id));
        }

        // ---- generator stamping ----

        private static FightOptionArmySource Source(int fightNumber, string factionId) =>
            new()
            {
                FightNumber = fightNumber,
                EnemyFactionId = factionId,
                BuildBoard = () => BoardWithRifles(fightNumber)
            };

        private static BoardState BoardWithRifles(int count)
        {
            var board = new BoardState(TestBoards.CombatLayout);
            for (int i = 0; i < count; i++)
                Assert.IsTrue(
                    board.TryPlace(TestPieces.RifleSquad(), new GridCoord(i, 0), $"enemy_{i}").Success,
                    $"rifle {i} must place");
            return board;
        }

        [Test]
        public void Generate_StampsEachOptionWithAThemeFromItsOwnPool()
        {
            var armies = new List<FightOptionArmySource>
            {
                Source(1, "neutral"), Source(2, "crimson_legion"),
                Source(1, "ash_wraiths"), Source(2, "neutral")
            };

            var options = FightOptionGenerator.Generate(4242, 3, dread: 2, armies);

            foreach (var option in options)
                CollectionAssert.Contains(
                    ArenaThemes.HomeThemes(option.EnemyFactionId).ToList(), option.ThemeId,
                    $"tier {option.Tier}: theme must come from the option's pool");
        }

        [Test]
        public void Generate_ThemeStamping_IsDeterministic()
        {
            var armies = new List<FightOptionArmySource>
            {
                Source(1, "neutral"), Source(2, "crimson_legion"), Source(1, "ash_wraiths")
            };

            var first = FightOptionGenerator.Generate(1337, 5, dread: 3, armies)
                .Select(o => o.ThemeId).ToList();
            var second = FightOptionGenerator.Generate(1337, 5, dread: 3, armies)
                .Select(o => o.ThemeId).ToList();

            CollectionAssert.AreEqual(first, second);
        }
    }
}
