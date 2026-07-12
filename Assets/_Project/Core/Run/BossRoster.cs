using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Run
{
    /// <summary>One piece of a boss stage army, in enemy-template board space.</summary>
    public sealed class BossStagePlacement
    {
        public string PieceId { get; }
        public int X { get; }
        public int Y { get; }

        public BossStagePlacement(string pieceId, int x, int y)
        {
            PieceId = pieceId;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Boss identity (commander name, twist, enemy pool) decoupled from boss stage:
    /// three stage loadouts of escalating strength (≈ old fights 4 / 7 / 10). The run
    /// meets bosses in a seeded hidden order; the stage is how many bosses fell before.
    /// </summary>
    public sealed class BossDefinition
    {
        public string BossId { get; init; }
        public string EnemyFactionId { get; init; }
        public string DisplayName { get; init; }
        public string TwistId { get; init; }

        /// <summary>Exactly <see cref="DreadRules.BossCount"/> loadouts, index = stage.</summary>
        public IReadOnlyList<IReadOnlyList<BossStagePlacement>> StageLoadouts { get; init; }
    }

    /// <summary>
    /// Code-authored boss roster v1 (no SO pipeline). Loadouts recombine placements
    /// from the shipped IronMarch enemy templates, so every (piece, anchor) pair is
    /// known-good on the 6x6 combat board. Crimson Legion / Ash Wraiths have faction
    /// assets but no pieces of their own yet, so their armies are rifleman-fallback
    /// compositions until their content passes land.
    /// </summary>
    public static class BossRoster
    {
        public const string MilitiaWarden = "boss_militia_warden";
        public const string CrimsonMarshal = "boss_crimson_marshal";
        public const string WraithHarbinger = "boss_wraith_harbinger";

        public static readonly IReadOnlyList<BossDefinition> All = new[]
        {
            new BossDefinition
            {
                BossId = MilitiaWarden,
                EnemyFactionId = "neutral",
                DisplayName = "Militia Warden",
                TwistId = TwistCatalog.EndlessMuster,
                StageLoadouts = new IReadOnlyList<BossStagePlacement>[]
                {
                    // ≈ fight 4: entrenched conscript line.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 4, 4),
                        P("conscript_rifleman", 5, 4), P("field_medic", 4, 5),
                        P("machine_gun_nest", 0, 4)),
                    // ≈ fight 7: the line plus veterans and mortars.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 4, 4),
                        P("conscript_rifleman", 5, 4), P("field_medic", 4, 5),
                        P("machine_gun_nest", 0, 4), P("enlisted_rifleman", 3, 5),
                        P("bulwark_squad", 5, 5), P("ironclad_mortars", 0, 0)),
                    // ≈ fight 10: the full march.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 4, 4),
                        P("conscript_rifleman", 5, 4), P("field_medic", 2, 5),
                        P("machine_gun_nest", 0, 4), P("enlisted_rifleman", 3, 5),
                        P("bulwark_squad", 5, 5), P("ironclad_mortars", 0, 0),
                        P("ironmarch_iron_horse", 2, 0), P("ironclad_marksman", 4, 5),
                        P("ironclad_field_marshal", 2, 4))
                }
            },
            new BossDefinition
            {
                BossId = CrimsonMarshal,
                EnemyFactionId = "crimson_legion",
                DisplayName = "Crimson Marshal",
                TwistId = TwistCatalog.IronDiscipline,
                StageLoadouts = new IReadOnlyList<BossStagePlacement>[]
                {
                    // ≈ fight 4-5: assault veterans behind a gun nest.
                    Loadout(
                        P("conscript_rifleman", 4, 4), P("conscript_rifleman", 5, 4),
                        P("enlisted_rifleman", 3, 5), P("bulwark_squad", 5, 5),
                        P("machine_gun_nest", 0, 4)),
                    // ≈ fight 7: armor joins the push.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 5, 4),
                        P("field_medic", 4, 5), P("machine_gun_nest", 0, 4),
                        P("enlisted_rifleman", 3, 5), P("bulwark_squad", 5, 5),
                        P("ironmarch_iron_horse", 2, 0)),
                    // ≈ fight 10: the legion in full battle order.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 4, 4),
                        P("conscript_rifleman", 5, 4), P("field_medic", 2, 5),
                        P("machine_gun_nest", 0, 4), P("enlisted_rifleman", 3, 5),
                        P("bulwark_squad", 5, 5), P("ironclad_mortars", 0, 0),
                        P("ironmarch_iron_horse", 2, 0), P("ironclad_field_marshal", 2, 4))
                }
            },
            new BossDefinition
            {
                BossId = WraithHarbinger,
                EnemyFactionId = "ash_wraiths",
                DisplayName = "Wraith Harbinger",
                TwistId = TwistCatalog.DeathlessCold,
                StageLoadouts = new IReadOnlyList<BossStagePlacement>[]
                {
                    // ≈ fight 4: a thin line screened by a marksman.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 4, 4),
                        P("conscript_rifleman", 5, 4), P("field_medic", 4, 5),
                        P("ironclad_marksman", 1, 5)),
                    // ≈ fight 7: long guns behind the line.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 4, 4),
                        P("conscript_rifleman", 5, 4), P("field_medic", 4, 5),
                        P("machine_gun_nest", 0, 4), P("ironclad_marksman", 1, 5),
                        P("ironclad_mortars", 0, 0)),
                    // ≈ fight 9-10: the cold host entire.
                    Loadout(
                        P("conscript_rifleman", 3, 4), P("conscript_rifleman", 5, 4),
                        P("field_medic", 2, 5), P("machine_gun_nest", 0, 4),
                        P("enlisted_rifleman", 3, 5), P("bulwark_squad", 5, 5),
                        P("ironclad_mortars", 0, 0), P("ironmarch_iron_horse", 2, 0),
                        P("ironclad_marksman", 1, 5), P("ironclad_field_marshal", 4, 4))
                }
            }
        };

        private static BossStagePlacement P(string pieceId, int x, int y) =>
            new BossStagePlacement(pieceId, x, y);

        private static IReadOnlyList<BossStagePlacement> Loadout(params BossStagePlacement[] placements) =>
            placements;

        public static BossDefinition Get(string bossId)
        {
            var boss = All.FirstOrDefault(b => b.BossId == bossId);
            if (boss == null)
                throw new InvalidOperationException($"Unknown boss id '{bossId}'.");
            return boss;
        }

        /// <summary>
        /// The run's boss order: a seeded permutation of the three boss ids, derived
        /// (never persisted) via the "bosses" sub-stream. Same seed, same order.
        /// </summary>
        public static string[] GetBossOrder(int runSeed)
        {
            var order = All.Select(b => b.BossId).ToArray();
            var rng = SeedStreams.Stream(runSeed, "bosses");
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            return order;
        }

        /// <summary>Builds the stage's enemy board the same way EnemyTemplateSO.BuildBoard
        /// does (unzoned combat board, deterministic instance ids).</summary>
        public static BoardState BuildStageBoard(
            BossDefinition boss,
            int stage,
            ContentRegistry registry,
            int boardSize = 6)
        {
            if (boss == null)
                throw new ArgumentNullException(nameof(boss));
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            stage = Math.Clamp(stage, 0, boss.StageLoadouts.Count - 1);
            var board = new BoardState(BoardLayout.CreateCombatBoard(boardSize));
            foreach (var placement in boss.StageLoadouts[stage])
            {
                int x = Math.Clamp(placement.X, 0, boardSize - 1);
                int y = Math.Clamp(placement.Y, 0, boardSize - 1);
                var result = board.TryPlace(
                    registry.GetById(placement.PieceId),
                    new GridCoord(x, y),
                    $"enemy_{placement.PieceId}_{x}_{y}");
                if (!result.Success)
                    throw new InvalidOperationException(
                        $"Boss '{boss.BossId}' stage {stage}: failed to place '{placement.PieceId}' at ({x},{y}): {result.Reason}");
            }

            return board;
        }
    }
}
