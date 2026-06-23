using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public static class TutorialBalanceFixtures
    {
        public const int SeedSweepCount = 40;
        public const float MinReachRate = 0.90f;
        public const float MinFight1PauseTwoReachRate = 0.82f;
        public const float MinFight2PauseTwoReachRate = 0.82f;
        /// <summary>Rough fraction of attack ticks that land full hits or grazes (not clean misses).</summary>
        public const float EstimatedEffectiveFireRate = 0.75f;

        /// <summary>
        /// Rough damage two conscripts focus-fire during in-range grind ticks (ballistic vs light, medium cooldown).
        /// Scaled by <see cref="EstimatedEffectiveFireRate"/> for accuracy variance.
        /// </summary>
        public static int EstimateTwoConscriptGrindDamage(int inRangeTicks = 170, int damagePerHit = 12) =>
            (int)(EstimatedEffectiveFireRate * (inRangeTicks / 3) * 2 * damagePerHit);

        public static BoardState BuildReferencePlayerBoard(ContentDatabase database, int fightIndex = 1, bool includeRifle = true)
        {
            var faction = database.GetFaction(FactionIds.IronVanguard);
            Assert.NotNull(faction);

            var board = new BoardState(faction.CreateBoardLayout());
            var hq = GetPiece(database, "ironmarch_hq");
            var conscript = GetPiece(database, "conscript_rifleman");
            Assert.NotNull(hq);
            Assert.NotNull(conscript);

            Assert.IsTrue(board.TryPlace(hq, new GridCoord(0, 4), "hq_player").Success);
            Assert.IsTrue(board.TryPlace(conscript, SupportAnchor(faction.rearCols, 1), "conscript_1").Success);
            Assert.IsTrue(board.TryPlace(conscript, SupportAnchor(faction.rearCols, 1, y: 6), "conscript_2").Success);

            if (includeRifle)
            {
                var rifle = GetPiece(database, "rifle_squad");
                Assert.NotNull(rifle);
                Assert.IsTrue(board.TryPlace(rifle, TestBoards.FrontLineAnchor(4), "rifle_1").Success);
            }

            return board;
        }

        /// <summary>Support-zone column for layouts with variable rear width (iron_vanguard uses 4 rear cols).</summary>
        private static GridCoord SupportAnchor(int rearCols, int columnOffset, int y = 4) =>
            new(rearCols + columnOffset, y);

        public static BoardState BuildEnemyBoard(ContentDatabase database, int fightIndex)
        {
            var faction = database.GetFaction(FactionIds.IronVanguard);
            var template = database.GetEnemyTemplate(fightIndex);
            Assert.NotNull(template);
            var board = template.BuildBoard(faction, database.BuildRegistry());
            var normalized = new BoardState(board.Layout);
            foreach (var piece in board.Pieces)
            {
                var result = normalized.TryPlace(
                    WithLegacySynergyFallbackAbilities(piece.Definition),
                    piece.Anchor,
                    piece.InstanceId,
                    piece.Rotation);
                Assert.IsTrue(result.Success, result.Reason);
            }

            return normalized;
        }

        /// <summary>Grind finished and pause #2 is available (fight still active).</summary>
        public static bool ReachesPauseTwo(BoardState player, BoardState enemy, int seed)
        {
            var run = RunThroughGrind(player, enemy, seed);
            return !run.IsFightOver
                   && run.AwaitingCommand
                   && run.CheckpointsFired >= 2;
        }

        /// <summary>Player was not eliminated during grind (early win or pause #2 both pass).</summary>
        public static bool SurvivedWithoutLossDuringGrind(BoardState player, BoardState enemy, int seed)
        {
            var run = RunThroughGrind(player, enemy, seed);
            if (run.IsFightOver)
                return run.PlayerWon && !run.IsDraw;

            return run.AwaitingCommand && run.CheckpointsFired >= 2;
        }

        public static float MeasurePauseTwoReachRate(int fightIndex, ContentDatabase database, int seedBase = 5000)
        {
            // Fight 1 uses the design-spec probe (2 conscripts, no rifle) so grind DPS stays low enough
            // for durable enemy lines to survive the full segment and reach pause #2.
            var player = fightIndex == 1
                ? BuildReferencePlayerBoard(database, includeRifle: false)
                : BuildReferencePlayerBoard(database, fightIndex);
            var enemy = BuildEnemyBoard(database, fightIndex);
            return MeasureRate((p, e, s) => ReachesPauseTwo(p, e, s), player, enemy, seedBase);
        }

        public static float MeasureSurvivalRate(int fightIndex, ContentDatabase database, int seedBase = 5000)
        {
            var player = BuildReferencePlayerBoard(database, fightIndex);
            var enemy = BuildEnemyBoard(database, fightIndex);
            return MeasureRate((p, e, s) => SurvivedWithoutLossDuringGrind(p, e, s), player, enemy, seedBase);
        }

        private static float MeasureRate(
            System.Func<BoardState, BoardState, int, bool> predicate,
            BoardState player,
            BoardState enemy,
            int seedBase)
        {
            int pass = 0;
            for (int i = 0; i < SeedSweepCount; i++)
            {
                if (predicate(player, enemy, seedBase + i))
                    pass++;
            }

            return pass / (float)SeedSweepCount;
        }

        private static TickCombatRun RunThroughGrind(BoardState player, BoardState enemy, int seed)
        {
            var run = TickCombatRun.Start(player, enemy, seed, authority: 0);

            run.Continue(new List<PhaseCommand>());
            if (run.IsFightOver)
                return run;

            run.Continue(new List<PhaseCommand>
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.DisciplinedFire,
                    SourcePieceId = "player_tactic"
                }
            });

            return run;
        }

        private static PieceDefinition GetPiece(ContentDatabase database, string pieceId) =>
            WithLegacySynergyFallbackAbilities(database.Pieces.First(p => p.id == pieceId).ToCore());

        private static PieceDefinition WithLegacySynergyFallbackAbilities(PieceDefinition piece)
        {
            if (piece.SynergyTags == null || piece.SynergyTags.Count == 0)
                return piece;
            if (piece.Abilities != null && piece.Abilities.Count > 0)
                return piece;

            var abilities = new List<PieceAbilityDefinition>();
            foreach (var tag in piece.SynergyTags)
            {
                switch (tag)
                {
                    case GameTagIds.Medic:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_medic_adjacent_infantry_armor_plus_one",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                            Stat = SynergyStat.ArmorType,
                            ModType = SynergyModType.Flat,
                            Magnitude = 1
                        });
                        break;
                    case GameTagIds.Command:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_command_adjacent_artillery_damage_plus_two",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery },
                            Stat = SynergyStat.Damage,
                            ModType = SynergyModType.Flat,
                            Magnitude = 2
                        });
                        break;
                    case GameTagIds.Echo:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_echo_adjacent_stealth_damage_plus_one",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = new NeighborFilter { AbilityTagId = GameTagIds.Stealth },
                            Stat = SynergyStat.Damage,
                            ModType = SynergyModType.Flat,
                            Magnitude = 1
                        });
                        break;
                    case GameTagIds.Inspiring:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_inspiring_adjacent_move_charge_plus_five",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = NeighborFilter.Any,
                            Stat = SynergyStat.MoveChargePercent,
                            ModType = SynergyModType.Flat,
                            Magnitude = 5
                        });
                        break;
                }
            }

            return abilities.Count == 0
                ? piece
                : TestPieces.With(piece, abilities: abilities);
        }
    }
}
