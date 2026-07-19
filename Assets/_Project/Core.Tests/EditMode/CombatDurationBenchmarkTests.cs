using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>Duration measurement harness (2026-07-19 balance pass A): how long do fights
    /// actually run, in sim ticks, across the 10-fight ladder? Player boards model a plausible
    /// run: the faction's starting loadout (combat units only, buildings skipped — they live on
    /// the HQ board) plus ~1 shop purchase per round (fight n carries n-1 extra units, drawn
    /// cheapest-first from the faction's own shop-pool roster, cycling). Enemy is the next
    /// faction's authored template for that fight. This is a MEASUREMENT harness, not a gate —
    /// it always passes and prints the table; duration bands become assertions in a later pass
    /// (Balance D).</summary>
    public sealed class CombatDurationBenchmarkTests
    {
        /// <summary>Sim cadence for the human-readable column: 10 ticks ≈ 1 second of combat.</summary>
        private const int TicksPerSecond = 10;

        /// <summary>Safety break for the Continue loop — the gas anti-stall ends every fight
        /// long before this, so tripping it means the sim regressed into a true stall.</summary>
        private const int MaxContinueIterations = 12000;

        private static readonly int[] Seeds = { 1, 2, 3 };

        /// <summary>4 player factions; each fights the NEXT faction in this list (variety
        /// without a full 8x8 matrix explosion).</summary>
        private static readonly string[] PlayerFactions =
        {
            FactionIds.IronmarchUnion,
            FactionIds.OathbornAccord,
            FactionIds.DustScourge,
            FactionIds.AshenCovenant
        };

        private ContentDatabase _database;
        private ContentRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
                return; // self-ignores via RequireDatabase

            _registry = _database.BuildRegistry();
        }

        [Test]
        [Category("Benchmark")]
        public void CombatDurations_Report()
        {
            RequireDatabase();

            var log = new StringBuilder();
            log.AppendLine("=== Combat Duration Benchmark (ticks per fight, 10 ticks ~ 1s) ===");
            log.AppendLine("Player board = starting loadout (combat units) + (n-1) cheapest-roster purchases; enemy = next faction's fight-n template.");
            log.AppendLine();
            log.AppendLine($"{"player faction",-18} {"vs",-18} {"n",2} {"seed",4} {"pTotal",6} {"eTotal",6} {"ticks",6} {"simSec",7} result");

            var ticksByFight = new Dictionary<int, List<int>>();
            var winsByFight = new Dictionary<int, int>();
            var runsByFight = new Dictionary<int, int>();
            for (int fight = 1; fight <= 10; fight++)
            {
                ticksByFight[fight] = new List<int>();
                winsByFight[fight] = 0;
                runsByFight[fight] = 0;
            }

            for (int f = 0; f < PlayerFactions.Length; f++)
            {
                string playerFactionId = PlayerFactions[f];
                string enemyFactionId = PlayerFactions[(f + 1) % PlayerFactions.Length];

                var playerFaction = _database.GetFaction(playerFactionId);
                var enemyFaction = _database.GetFaction(enemyFactionId);
                Assert.NotNull(playerFaction, $"missing FactionSO '{playerFactionId}'");
                Assert.NotNull(enemyFaction, $"missing FactionSO '{enemyFactionId}'");

                var roster = BuildGrowthRoster(playerFactionId);
                Assert.Greater(roster.Count, 0,
                    $"'{playerFactionId}' has no shop-pool combat units to model growth with");

                for (int fight = 1; fight <= 10; fight++)
                {
                    var template = _database.GetEnemyTemplate(fight, enemyFactionId);
                    Assert.NotNull(template, $"missing enemy template fight {fight} for '{enemyFactionId}'");

                    for (int s = 0; s < Seeds.Length; s++)
                    {
                        int seed = Seeds[s];

                        // Fresh boards per sim — TickCombatRun consumes them.
                        var playerBoard = BuildPlayerBoard(playerFaction, roster, fight);
                        var enemyBoard = template.BuildBoard(enemyFaction, _registry);

                        int pTotal = ArmyStrengthCalculator.Evaluate(playerBoard).EffectiveTotal;
                        int eTotal = ArmyStrengthCalculator.Evaluate(enemyBoard).EffectiveTotal;

                        var run = TickCombatRun.Start(playerBoard, enemyBoard, seed);
                        var result = run.Continue(Array.Empty<PhaseCommand>());
                        int guard = 0;
                        while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                        {
                            guard++;
                            if (guard > MaxContinueIterations)
                                Assert.Fail(
                                    $"safety break: {playerFactionId} fight {fight} seed {seed} never completed after {MaxContinueIterations} Continue calls");
                            result = run.Continue(Array.Empty<PhaseCommand>());
                        }

                        int ticks = run.GlobalTick;
                        string outcome = run.IsDraw ? "draw" : (run.PlayerWon ? "player" : "enemy");

                        ticksByFight[fight].Add(ticks);
                        runsByFight[fight]++;
                        if (!run.IsDraw && run.PlayerWon)
                            winsByFight[fight]++;

                        log.AppendLine(
                            $"{playerFactionId,-18} {enemyFactionId,-18} {fight,2} {seed,4} {pTotal,6} {eTotal,6} {ticks,6} {(double)ticks / TicksPerSecond,6:0.0}s {outcome}");
                    }
                }
            }

            var summary = new StringBuilder();
            summary.AppendLine("=== Per-fight summary (4 factions x 3 seeds = 12 sims each) ===");
            for (int fight = 1; fight <= 10; fight++)
            {
                double medianTicks = Median(ticksByFight[fight]);
                double winRate = runsByFight[fight] > 0
                    ? (double)winsByFight[fight] / runsByFight[fight]
                    : 0.0;
                summary.AppendLine(
                    $"fight {fight,2}: median {medianTicks,6:0.#} ticks ({medianTicks / TicksPerSecond,5:0.0}s), player win rate {winRate:P0}");
            }

            log.AppendLine();
            log.Append(summary);
            TestContext.WriteLine(log.ToString());
            UnityEngine.Debug.Log(log.ToString());

            // Measurement harness, not a gate (yet) — duration band assertions land in Balance D.
            Assert.Pass(summary.ToString());
        }

        /// <summary>Faction's purchasable combat units, cheapest first (RarityPricing.BaseCost
        /// by rarity — the core PieceDefinition has no direct cost field), then id for
        /// determinism. This is the deck the "1 purchase per round" growth model cycles.</summary>
        private List<PieceDefinition> BuildGrowthRoster(string factionId)
        {
            return _database.Pieces
                .Where(p => p != null && p.includeInShopPool)
                .Select(p => p.ToCore())
                .Where(p => p.FactionId == factionId
                            && ManpowerCalculator.CountsTowardFielding(p)
                            && BoardPlacementRules.ResolveTargetBoard(p) == BoardKind.Combat)
                .OrderBy(p => RarityPricing.BaseCost(p.Rarity))
                .ThenBy(p => p.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>Starting loadout (combat units only — buildings resolve to the HQ board and
        /// are skipped, mirroring RunOrchestrator.ApplyStartingLoadout's board routing) plus
        /// (fightNumber - 1) growth units cycled from the roster. Authored anchors are
        /// preferences; occupied/illegal falls back to a forward scan; no-fit pieces are
        /// skipped — same tolerance as the real loadout code.</summary>
        private BoardState BuildPlayerBoard(FactionSO faction, List<PieceDefinition> roster, int fightNumber)
        {
            var board = new BoardState(faction.CreateCombatBoardLayout());

            var entries = faction.startingPieces;
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.pieceId) ||
                        !_registry.TryGetById(entry.pieceId, out var piece))
                        continue;
                    if (BoardPlacementRules.ResolveTargetBoard(piece) == BoardKind.Hq)
                        continue;

                    string instanceId = $"start_{i}_{entry.pieceId}";
                    if (!board.TryPlace(piece, new GridCoord(entry.anchor.x, entry.anchor.y), instanceId).Success)
                        PlaceScan(board, piece, instanceId);
                }
            }

            for (int i = 0; i < fightNumber - 1; i++)
            {
                var piece = roster[i % roster.Count];
                PlaceScan(board, piece, $"growth_{i}_{piece.Id}");
            }

            return board;
        }

        /// <summary>First-legal-cell scan (y-major, matching RunOrchestrator). False = no room;
        /// caller skips the piece.</summary>
        private static bool PlaceScan(BoardState board, PieceDefinition piece, string instanceId)
        {
            for (int y = 0; y < board.Layout.Height; y++)
                for (int x = 0; x < board.Layout.Width; x++)
                    if (board.TryPlace(piece, new GridCoord(x, y), instanceId).Success)
                        return true;
            return false;
        }

        private static double Median(List<int> values)
        {
            if (values.Count == 0)
                return 0.0;
            var sorted = new List<int>(values);
            sorted.Sort();
            int n = sorted.Count;
            return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }

        private void RequireDatabase()
        {
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
        }
    }
}
