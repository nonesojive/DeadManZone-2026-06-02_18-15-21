using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
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
    /// builds every stage). 2026-07-15 faction-roster-v1 Wave 2: crimson_legion /
    /// ash_wraiths (enemy-only pools, never had pieces of their own) are retired;
    /// Crimson Assembly and Ashen Covenant now own the same two boss slots.
    /// 2026-07-15 faction-roster-v1: piece ids updated to the new Neutral/IronMarch
    /// roster (conscript_rifleman→conscript_rifles, enlisted_rifleman→shock_sergeant,
    /// bulwark_squad→iron_guard, ironclad_mortars→field_mortar_team,
    /// ironclad_marksman→marksman_doctrine_officer, ironclad_field_marshal→
    /// forward_observer, ironmarch_iron_horse→breakthrough_tank). Footprints were
    /// chosen 1:1 with the old shapes so these hand-authored anchors keep landing
    /// unchanged — see IronmarchUnionContentFactory.Pieces.cs PROVISIONAL notes.
    ///
    /// Wave 5 (2026-07-17): Crimson Marshal and Wraith Harbinger's loadouts are
    /// rebuilt from Crimson Assembly's and Ashen Covenant's OWN 12-piece rosters
    /// (previously IronMarch/Neutral fallback pieces, flagged above as W5 scope —
    /// now done). Boss-faction selection is UNCHANGED and stays the smallest
    /// coherent rule: 3 fixed boss identities (neutral / crimson_assembly /
    /// ashen_covenant), each already a faction present in the Fight Option rotation
    /// (FactionIds.Playable / ContentDatabase.PlayableFactionIds), so "the boss you
    /// meet matches a faction present in the rotation" falls out for free — no new
    /// per-faction boss needed for the other 5 factions; BossRoster stays a run-clock
    /// concept (3 Dread thresholds), separate from the 8-faction fight rotation.
    /// Anchors reuse EnemyTemplateAnchors' 9-slot grid (see that class's doc comment
    /// for the collision-avoidance invariant) so these loadouts are trivially legal
    /// and each stage is a strict superset of the previous (guarantees the strictly
    /// increasing EffectiveTotal BalancePassTests.BossStages_* requires).
    /// </summary>
    public static class BossRoster
    {
        /// <summary>PROVISIONAL 2026-07-19 owner spec (deep balance pass): a boss stage is a
        /// clear step up — 1.5x the concurrent normal-fight ladder strength
        /// (<see cref="EnemyLadder.TargetStrength"/> at the stage's Dread threshold's
        /// <see cref="DreadRules.FightEquivalent"/>).</summary>
        public const float BossStrengthRatio = 1.5f;

        public const string MilitiaWarden = "boss_militia_warden";
        public const string CrimsonMarshal = "boss_crimson_marshal";
        public const string WraithHarbinger = "boss_wraith_harbinger";

        // EnemyTemplateAnchors' 9-slot grid, duplicated here as plain ints (Core/Run cannot
        // reference Data/Editor — that assembly is editor-only and Core must stay Unity-clean).
        // P1=(0,0) P2=(2,0) P3=(4,0) P4=(0,2) P5=(2,2) P6=(4,2) P7=(0,4) P8=(2,4) P9=(4,4).

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
                EnemyFactionId = FactionIds.CrimsonAssembly,
                DisplayName = "Crimson Marshal",
                TwistId = TwistCatalog.IronDiscipline,
                // Wave 5: rebuilt from Crimson Assembly's own roster (was IronMarch/Neutral
                // fallback pieces). Mirrors CrimsonAssemblyEnemyFactory's fights 5→7→9 —
                // suppression teams and the assembly line, then Fire-Plan Officer/Scout
                // Tankette join, then the Marshal fields BOTH sanctioned rare tanks
                // (§1.6's "two rare tanks is a sanctioned archetype stack") as her finale.
                // Anchors are 9 distinct slots from EnemyTemplateAnchors' grid (max one
                // piece per slot) — each stage is a strict superset of the last.
                StageLoadouts = new IReadOnlyList<BossStagePlacement>[]
                {
                    // ≈ fight 4-5: suppression teams behind the assembly line.
                    Loadout(
                        P("suppression_team", 4, 2), P("suppression_team", 2, 4),
                        P("assembly_trooper", 2, 2), P("ballistics_analyst", 0, 2)),
                    // ≈ fight 6-7: hazmat vanguard, Fire-Plan Officer, and the Scout Tankette join.
                    Loadout(
                        P("suppression_team", 4, 2), P("suppression_team", 2, 4),
                        P("assembly_trooper", 2, 2), P("ballistics_analyst", 0, 2),
                        P("hazmat_vanguard", 4, 4), P("fire_plan_officer", 0, 4),
                        P("scout_tankette", 4, 0)),
                    // ≈ fight 8-9: both sanctioned rare tanks debut together — the
                    // Marshal's clinical-optimization doctrine at full strength.
                    Loadout(
                        P("suppression_team", 4, 2), P("suppression_team", 2, 4),
                        P("assembly_trooper", 2, 2), P("ballistics_analyst", 0, 2),
                        P("hazmat_vanguard", 4, 4), P("fire_plan_officer", 0, 4),
                        P("scout_tankette", 4, 0), P("vanquisher_doctrine_tank", 0, 0),
                        P("stiller_suppression_platform", 2, 0))
                }
            },
            new BossDefinition
            {
                BossId = WraithHarbinger,
                EnemyFactionId = FactionIds.AshenCovenant,
                DisplayName = "Wraith Harbinger",
                TwistId = TwistCatalog.DeathlessCold,
                // Wave 5: rebuilt from Ashen Covenant's own roster (was IronMarch/Neutral
                // fallback pieces). Mirrors AshenCovenantEnemyFactory's fights 5→7→9 — the
                // fanatic swarm, then Reliquary Bearer/Firebrand Vicar join, then Saint of
                // the Embers and The Ash Martyr headline the final host. Each stage is a
                // strict superset of the last.
                StageLoadouts = new IReadOnlyList<BossStagePlacement>[]
                {
                    // ≈ fight 4-5: a thin line of ash acolytes screened by a hymnal leader.
                    Loadout(
                        P("ash_acolyte", 4, 2), P("ash_acolyte", 2, 4),
                        P("torchbearer", 2, 2), P("hymnal_leader", 0, 2)),
                    // ≈ fight 6-7: the penitent line holds while Reliquary Bearer and
                    // Firebrand Vicar strengthen the fanatics.
                    Loadout(
                        P("ash_acolyte", 4, 2), P("ash_acolyte", 2, 4),
                        P("torchbearer", 2, 2), P("hymnal_leader", 0, 2),
                        P("penitent", 4, 4), P("reliquary_bearer", 0, 4),
                        P("firebrand_vicar", 2, 0)),
                    // ≈ fight 8-9: the Saint and the Martyr lead the revolution of cinders.
                    Loadout(
                        P("ash_acolyte", 4, 2), P("ash_acolyte", 2, 4),
                        P("torchbearer", 2, 2), P("hymnal_leader", 0, 2),
                        P("penitent", 4, 4), P("reliquary_bearer", 0, 4),
                        P("firebrand_vicar", 2, 0), P("saint_of_the_embers", 0, 0),
                        P("the_ash_martyr", 4, 0))
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

        /// <summary>
        /// PROVISIONAL 2026-07-19 owner spec: target EffectiveTotal for a boss stage
        /// (0-based). Stage s fires at Dread threshold <see cref="DreadRules.NextThreshold"/>(s)
        /// = 6/12/18, whose <see cref="DreadRules.FightEquivalent"/> is fight 4/7/10 (clamped
        /// to the authored 1..10), so targets are round(1.5 x T(fight)):
        /// stage 0 → 1.5 x 354 = 531, stage 1 → 1.5 x 628 = 942, stage 2 → 1.5 x 1112 = 1668.
        /// </summary>
        public static int StageTargetStrength(int stage)
        {
            int threshold = DreadRules.NextThreshold(
                Math.Clamp(stage, 0, DreadRules.BossCount - 1));
            int fight = Math.Clamp(DreadRules.FightEquivalent(threshold), 1, 10);
            return (int)Math.Round(BossStrengthRatio * EnemyLadder.TargetStrength(fight));
        }

        /// <summary>Builds the stage's enemy board the same way EnemyTemplateSO.BuildBoard
        /// does (unzoned combat board, deterministic instance ids), then normalizes it onto
        /// the boss ladder (<see cref="StageTargetStrength"/>) via a uniform per-piece
        /// StatScale — scaling is uniform per stage board, so the authored strict-superset
        /// stage composition survives (supersets remain supersets).</summary>
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

            // PROVISIONAL 2026-07-19 owner spec: boss step-up — normalize the stage board to
            // 1.5x the concurrent ladder target (engines-on Evaluate basis, same as templates).
            // Boss boards get a wider solver ceiling (4.0): ability-heavy stage compositions
            // (e.g. wraith_harbinger stage 0) rate sub-linearly in StatScale, and the default
            // 2.5 cap left stage 0 at 450 vs its 531 target on the first band-test run.
            BoardStrengthScaler.SolveScale(board, StageTargetStrength(stage), maxScale: 4.0f);
            return board;
        }
    }
}
