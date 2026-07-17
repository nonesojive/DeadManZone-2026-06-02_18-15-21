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

        /// <summary>2026-07-15 faction-roster-v1 §2.9/§4: MoraleRules.IsDeathShockInverted keys
        /// off it directly (smaller than adding a per-piece flag for a whole-faction passive).
        /// Wave 2 landed its full 12-piece content pass (AshenCovenantContentFactory).</summary>
        public const string AshenCovenant = "ashen_covenant";

        // ---- W1b (2026-07-16, faction-roster-v1 §1.9/§4) economy/shop passives key off
        // these directly (FactionPassives). Wave 2 landed each of these factions' full content
        // pass (12 pieces + FactionSO) via their own *ContentFactory.
        public const string OathbornAccord = "oathborn_accord";
        public const string ParadoxEngine = "paradox_engine";
        public const string BlightbornPact = "blightborn_pact";
        public const string CrimsonAssembly = "crimson_assembly";

        /// <summary>
        /// Playable factions in selection order (matches ContentDatabase.PlayableFactionIds
        /// contract). 2026-07-15 faction-roster-v1 Wave 2: all 8 factions now have a full
        /// content pass (12 pieces + FactionSO each) — see AllFactionsContentFactory. Faction
        /// SELECT UI gating (MetaProgressionService.IsFactionUnlocked / MainMenuController) is
        /// a separate, later concern (faction-select overhaul) and is untouched here.
        /// </summary>
        public static readonly string[] Playable =
        {
            IronmarchUnion, DustScourge, CartelOfEchoes, OathbornAccord,
            ParadoxEngine, BlightbornPact, CrimsonAssembly, AshenCovenant,
        };

        /// <summary>
        /// Returns true if the given id is a playable faction.
        /// </summary>
        public static bool IsPlayable(string factionId) =>
            !string.IsNullOrEmpty(factionId) && Array.IndexOf(Playable, factionId) >= 0;
    }
}
