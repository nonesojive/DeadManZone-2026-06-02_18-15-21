using System.Collections.Generic;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Run
{
    /// <summary>
    /// Arena Theme keying (M4, roadmap "Arena Themes wave 1"). A theme is an environment
    /// DRESSING over the single shared board geometry — value structure, camera, flat
    /// combat strip and silhouette rules never change (CONTEXT.md "Arena Theme").
    ///
    /// Each enemy pool owns a small home-theme set; a Fight Option's theme rolls seeded
    /// from its pool's set at Front Report generation, and a Boss Fight always lands on
    /// its pool's signature ground. Wave 1 ships four of the canonical six: Trenchline,
    /// Fog Field, Ravaged Town, Wartorn Forest (Trench-dressing / Siege Ground later —
    /// append here and to a pool's set; ids are save-visible, never rename).
    /// </summary>
    public static class ArenaThemes
    {
        public const string Trenchline = "trenchline";
        public const string FogField = "fog_field";
        public const string RavagedTown = "ravaged_town";
        public const string WartornForest = "wartorn_forest";

        /// <summary>Fallback for legacy saves (null ThemeId) and unknown pools.</summary>
        public const string Default = Trenchline;

        public static readonly IReadOnlyList<string> All = new[]
        {
            Trenchline, FogField, RavagedTown, WartornForest
        };

        // Pool → home-theme set. Order matters: the seeded roll indexes into it,
        // so reordering (not just renaming) changes every seeded run's themes.
        private static readonly IReadOnlyList<string> NeutralHome = new[] { Trenchline, RavagedTown };
        // 2026-07-15 faction-roster-v1 Wave 2: crimson_legion/ash_wraiths (enemy-only, no
        // pieces of their own) are retired — Crimson Assembly and Ashen Covenant are the
        // playable factions that replace them as BossRoster's enemy-pool ids. Same theme
        // sets carried over unchanged (theming, not roster, is what these keys drive here).
        private static readonly IReadOnlyList<string> CrimsonHome = new[] { Trenchline, RavagedTown, WartornForest };
        private static readonly IReadOnlyList<string> WraithHome = new[] { FogField, WartornForest };
        // The shipped enemy TEMPLATES all carry pool id "ironmarch_union" until the
        // balance pass re-authors them onto the canonical pools (neutral / crimson_assembly
        // / ashen_covenant — today only bosses use those). Without this entry every normal
        // fight would sit on the Trenchline default and wave 1 would be boss-only.
        private static readonly IReadOnlyList<string> IronmarchHome = new[] { Trenchline, RavagedTown, WartornForest };
        private static readonly IReadOnlyList<string> DefaultHome = new[] { Default };

        /// <summary>Home-theme set for an enemy pool; unknown/null pools get the default.</summary>
        public static IReadOnlyList<string> HomeThemes(string enemyFactionId) => enemyFactionId switch
        {
            "neutral" => NeutralHome,
            "crimson_assembly" => CrimsonHome,
            "ashen_covenant" => WraithHome,
            "ironmarch_union" => IronmarchHome,
            _ => DefaultHome
        };

        /// <summary>The pool's signature ground — where its boss always fights.</summary>
        public static string SignatureTheme(string enemyFactionId) => enemyFactionId switch
        {
            "neutral" => Trenchline,
            "crimson_assembly" => RavagedTown,
            "ashen_covenant" => FogField,
            _ => Default
        };

        /// <summary>
        /// Seeded theme roll for one Fight Option slot. Own named sub-stream (never the
        /// generator's "options" stream) so stamping themes cannot perturb existing
        /// army/condition rolls — the SeedStreams invariant.
        /// </summary>
        public static string Roll(int runSeed, int roundIndex, int slot, string enemyFactionId)
        {
            var home = HomeThemes(enemyFactionId);
            if (home.Count == 1)
                return home[0];
            var rng = SeedStreams.Stream(runSeed, "arenaTheme", roundIndex, slot);
            return home[rng.NextInt(0, home.Count)];
        }

        /// <summary>Maps null/unknown ids (legacy saves, future content) to the default.</summary>
        public static string Normalize(string themeId)
        {
            if (string.IsNullOrEmpty(themeId))
                return Default;
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == themeId)
                    return themeId;
            }

            return Default;
        }
    }
}
