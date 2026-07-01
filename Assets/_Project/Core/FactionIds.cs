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
