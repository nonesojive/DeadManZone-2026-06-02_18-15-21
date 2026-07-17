using System;
using System.Linq;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 5 (2026-07-17): nine anchor points on the unzoned 6x6 enemy board, spaced 2 cells
    /// apart in both axes. Each anchor owns a 2x2 "room" (anchor .. anchor+(1,1)) that no other
    /// anchor's room touches, so ANY of this roster's footprints — Single, HorizontalPair,
    /// VerticalPair, or Square2x2 — can be dropped on any slot with zero collision math, no
    /// matter which slots are already occupied. (Triple3/tromino-footprint pieces don't fit a
    /// single room and are deliberately not used in these per-faction ladders — see each
    /// faction's *EnemyFactory class comment.)
    ///
    /// Used by every non-IronMarch faction's enemy ladder and by BossRoster's rebuilt Crimson
    /// Marshal / Wraith Harbinger loadouts. IronmarchEnemyFactory/BossRoster's original three
    /// loadouts predate this and keep their own hand-tuned anchors — untouched.
    /// </summary>
    internal static class EnemyTemplateAnchors
    {
        public static readonly Vector2Int P1 = new Vector2Int(0, 0);
        public static readonly Vector2Int P2 = new Vector2Int(2, 0);
        public static readonly Vector2Int P3 = new Vector2Int(4, 0);
        public static readonly Vector2Int P4 = new Vector2Int(0, 2);
        public static readonly Vector2Int P5 = new Vector2Int(2, 2);
        public static readonly Vector2Int P6 = new Vector2Int(4, 2);
        public static readonly Vector2Int P7 = new Vector2Int(0, 4);
        public static readonly Vector2Int P8 = new Vector2Int(2, 4);
        public static readonly Vector2Int P9 = new Vector2Int(4, 4);

        public static EnemyPiecePlacement Place(PieceDefinitionSO piece, Vector2Int anchor) =>
            DemoContentGenerator.Placement(piece, anchor.x, anchor.y);

        /// <summary>Replaces whichever placement already sits at <paramref name="anchor"/> with
        /// <paramref name="replacement"/> (falls back to appending if the anchor was unused) —
        /// the "upgrade a slot" half of every faction ladder's superset-growth guarantee: fight
        /// N+1 only ever adds a placement or replaces one with a higher-rarity piece, so
        /// ArmyStrengthCalculator.EffectiveTotal can never decrease fight over fight.</summary>
        public static EnemyPiecePlacement[] Swap(
            EnemyPiecePlacement[] source, Vector2Int anchor, PieceDefinitionSO replacement)
        {
            var result = (EnemyPiecePlacement[])source.Clone();
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i].anchor == anchor)
                {
                    result[i] = Place(replacement, anchor);
                    return result;
                }
            }
            return result.Append(Place(replacement, anchor)).ToArray();
        }
    }
}
