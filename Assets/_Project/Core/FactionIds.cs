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
        public const string IronVanguard = "iron_vanguard";
        public const string DustScourge = "dust_scourge";
        public const string CartelOfEchoes = "cartel_of_echoes";

        /// <summary>
        /// Playable factions in selection order (matches ContentDatabase.PlayableFactionIds contract).
        /// </summary>
        public static readonly string[] Playable =
        {
            IronVanguard,
            DustScourge,
            CartelOfEchoes
        };

        /// <summary>
        /// Returns true if the given id is one of the three playable factions.
        /// </summary>
        public static bool IsPlayable(string factionId) =>
            !string.IsNullOrEmpty(factionId) && Array.IndexOf(Playable, factionId) >= 0;
    }
}
