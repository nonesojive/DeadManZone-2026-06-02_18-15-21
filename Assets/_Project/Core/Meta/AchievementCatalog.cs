using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Meta
{
    public static class AchievementIds
    {
        public const string ClearGauntlet = "clear_gauntlet";
        public const string WinNoHqDamage = "win_no_hq_damage";
        public const string CriticalMassFive = "critical_mass_five";
        public const string SalvageHundred = "salvage_hundred";
        public const string EmergencyDraftUsed = "emergency_draft_used";
        public const string WinDustScourge = "win_dust_scourge";
        public const string WinCartel = "win_cartel_of_echoes";
        public const string WinIronmarch = "win_ironmarch";
        public const string TenFightsSurvived = "ten_fights_survived";
        public const string PerfectMorale = "perfect_morale_victory";
    }

    public readonly struct AchievementDefinition
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public string Description { get; init; }
        public string SteamApiName { get; init; }
    }

    public static class AchievementCatalog
    {
        public static readonly IReadOnlyList<AchievementDefinition> All = new[]
        {
            new AchievementDefinition
            {
                Id = AchievementIds.ClearGauntlet,
                DisplayName = "Gauntlet Cleared",
                Description = "Win a full 10-fight campaign.",
                SteamApiName = "ACH_CLEAR_GAUNTLET"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.WinNoHqDamage,
                DisplayName = "Untouched Command",
                Description = "Win a fight without HQ taking damage.",
                SteamApiName = "ACH_NO_HQ_DAMAGE"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.CriticalMassFive,
                DisplayName = "Critical Mass",
                Description = "Trigger critical mass bonuses 5 times in one run.",
                SteamApiName = "ACH_CRITICAL_MASS"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.SalvageHundred,
                DisplayName = "Salvage Master",
                Description = "Salvage 100 supplies in a single run.",
                SteamApiName = "ACH_SALVAGE_100"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.EmergencyDraftUsed,
                DisplayName = "Last Reserves",
                Description = "Use emergency draft to continue a run.",
                SteamApiName = "ACH_EMERGENCY_DRAFT"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.WinDustScourge,
                DisplayName = "Dust Storm Victor",
                Description = "Clear the gauntlet as Dust Scourge.",
                SteamApiName = "ACH_WIN_DUST"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.WinCartel,
                DisplayName = "Echoes of Victory",
                Description = "Clear the gauntlet as Cartel of Echoes.",
                SteamApiName = "ACH_WIN_CARTEL"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.WinIronmarch,
                DisplayName = "Iron Line Holds",
                Description = "Clear the gauntlet as Ironmarch Vanguard.",
                SteamApiName = "ACH_WIN_IRONMARCH"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.TenFightsSurvived,
                DisplayName = "Veteran",
                Description = "Complete 10 fights in a single run (win or lose).",
                SteamApiName = "ACH_TEN_FIGHTS"
            },
            new AchievementDefinition
            {
                Id = AchievementIds.PerfectMorale,
                DisplayName = "Unbroken Spirit",
                Description = "Win a campaign at 100 morale.",
                SteamApiName = "ACH_PERFECT_MORALE"
            }
        };

        public static bool TryFind(string id, out AchievementDefinition definition)
        {
            foreach (var item in All)
            {
                if (item.Id == id)
                {
                    definition = item;
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }
}
