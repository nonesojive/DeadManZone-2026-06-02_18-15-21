using System;

namespace DeadManZone.Core
{
    /// <summary>
    /// Centralized canonical string identifiers for factions.
    /// These strings are the persisted values used in saves, SOs, JSON, and content lookups.
    /// Replacing scattered literals eliminates typo risk and provides a single source of truth.
    /// (See codebase audit ROI item #1.)
    /// </summary>
    public static class FactionIds
    {
        public const string IronmarchUnion = "ironmarch_union";
        public const string DustScourge = "dust_scourge";
        public const string CartelOfEchoes = "cartel_of_echoes";

        /// <summary>2026-07-15 faction-roster-v1 §2.9/§4: the only other faction id wired this
        /// wave — MoraleRules.IsDeathShockInverted keys off it directly (smaller than adding a
        /// per-piece flag for a whole-faction passive). Content (roster/pass) lands later.</summary>
        public const string AshenCovenant = "ashen_covenant";

        // ---- W1b (2026-07-16, faction-roster-v1 §1.9/§4): economy/shop passives key off
        // these directly (FactionPassives) — no FactionSO asset exists for them yet, so
        // they are unreachable via StartNewRun until W2 lands their content pass.
        public const string OathbornAccord = "oathborn_accord";
        public const string ParadoxEngine = "paradox_engine";
        public const string BlightbornPact = "blightborn_pact";
        public const string CrimsonAssembly = "crimson_assembly";

        /// <summary>
        /// Playable factions in selection order (matches ContentDatabase.PlayableFactionIds contract).
        /// </summary>
        public static readonly string[] Playable =
        {
            IronmarchUnion,
        };

        /// <summary>
        /// Returns true if the given id is a playable faction.
        /// </summary>
        public static bool IsPlayable(string factionId) =>
            !string.IsNullOrEmpty(factionId) && Array.IndexOf(Playable, factionId) >= 0;
    }
}
