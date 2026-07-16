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
    /// three stage loadouts of escalating strength (≈ fights 4 / 6-7 / 8-9 after the
    /// 2026-07-12 balance pass trimmed stage-3 heat — a probed max-strength player
    /// board only DREW against the old fight-10-sized loadouts). The run meets
    /// bosses in a seeded hidden order; the stage is how many bosses fell before.
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
    /// Code-authored boss roster v1 (no SO pipeline). Loadouts follow the same
    /// anchor-legality rules as the IronMarch enemy templates (6x6 unzoned combat
    /// board; BuildStageBoard throws on any illegal placement, and BalancePassTests
    /// builds every stage). Crimson Legion / Ash Wraiths have faction assets but no
    /// pieces of their own yet, so their armies are rifleman-fallback compositions
    /// until their content passes land.
    /// 2026-07-15 faction-roster-v1: piece ids updated to the new Neutral/IronMarch
    /// roster (conscript_rifleman→conscript_rifles, enlisted_rifleman→shock_sergeant,
    /// bulwark_squad→iron_guard, ironclad_mortars→field_mortar_team,
    /// ironclad_marksman→marksman_doctrine_officer, ironclad_field_marshal→
    /// forward_observer, ironmarch_iron_horse→breakthrough_tank). Footprints were
    /// chosen 1:1 with the old shapes so these hand-authored anchors keep landing
    /// unchanged — see IronmarchUnionContentFactory.Pieces.cs PROVISIONAL notes.
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
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 4, 4),
                        P("conscript_rifles", 5, 4), P("field_medic", 4, 5),
                        P("machine_gun_nest", 0, 4)),
                    // ≈ fight 6-7: the line plus veterans and mortars.
                    Loadout(
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 4, 4),
                        P("conscript_rifles", 5, 4), P("field_medic", 4, 5),
                        P("machine_gun_nest", 0, 4), P("shock_sergeant", 3, 5),
                        P("iron_guard", 5, 5), P("field_mortar_team", 0, 0)),
                    // ≈ fight 8-9: the full muster on foot. Balance pass 2026-07-12:
                    // dropped the iron horse — a militia mass fields no armor, and the
                    // old fight-10-sized loadout left no win line for a strong player.
                    Loadout(
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 4, 4),
                        P("conscript_rifles", 5, 4), P("field_medic", 2, 5),
                        P("machine_gun_nest", 0, 4), P("shock_sergeant", 3, 5),
                        P("iron_guard", 5, 5), P("field_mortar_team", 0, 0),
                        P("marksman_doctrine_officer", 4, 5), P("forward_observer", 2, 4))
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
                        P("conscript_rifles", 4, 4), P("conscript_rifles", 5, 4),
                        P("shock_sergeant", 3, 5), P("iron_guard", 5, 5),
                        P("machine_gun_nest", 0, 4)),
                    // ≈ fight 5-6: armor joins the push.
                    Loadout(
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 5, 4),
                        P("field_medic", 4, 5), P("machine_gun_nest", 0, 4),
                        P("shock_sergeant", 3, 5), P("iron_guard", 5, 5),
                        P("breakthrough_tank", 2, 0)),
                    // ≈ fight 8: the legion in battle order behind its armor.
                    // Balance pass 2026-07-12: dropped the field marshal — the Marshal
                    // IS the commander; the iron horse stays because armor is the
                    // legion's identity.
                    Loadout(
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 4, 4),
                        P("conscript_rifles", 5, 4), P("field_medic", 2, 5),
                        P("machine_gun_nest", 0, 4), P("shock_sergeant", 3, 5),
                        P("iron_guard", 5, 5), P("field_mortar_team", 0, 0),
                        P("breakthrough_tank", 2, 0))
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
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 4, 4),
                        P("conscript_rifles", 5, 4), P("field_medic", 4, 5),
                        P("marksman_doctrine_officer", 1, 5)),
                    // ≈ fight 5-6: long guns behind the line.
                    Loadout(
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 4, 4),
                        P("conscript_rifles", 5, 4), P("field_medic", 4, 5),
                        P("machine_gun_nest", 0, 4), P("marksman_doctrine_officer", 1, 5),
                        P("field_mortar_team", 0, 0)),
                    // ≈ fight 7-8: the cold host — long guns and a screen. Balance
                    // pass 2026-07-12: dropped the iron horse; wraiths field ghosts
                    // and rifles, not armor, and stage 3 ran too hot with it.
                    Loadout(
                        P("conscript_rifles", 3, 4), P("conscript_rifles", 5, 4),
                        P("field_medic", 2, 5), P("machine_gun_nest", 0, 4),
                        P("shock_sergeant", 3, 5), P("iron_guard", 5, 5),
                        P("field_mortar_team", 0, 0), P("marksman_doctrine_officer", 1, 5),
                        P("forward_observer", 4, 4))
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
