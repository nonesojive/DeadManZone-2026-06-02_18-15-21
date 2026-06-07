namespace DeadManZone.Game
{
    /// <summary>Steamworks wrapper with local fallback when Steam is unavailable.</summary>
    public static class SteamIntegration
    {
        public static bool IsAvailable { get; private set; }

        public static void Initialize()
        {
            // Steamworks SDK integration point — disabled until SDK is wired.
            IsAvailable = false;
        }

        public static void UnlockAchievement(string steamApiName)
        {
            if (!IsAvailable || string.IsNullOrEmpty(steamApiName))
                return;

            // SteamUserStats.SetAchievement(steamApiName);
            // SteamUserStats.StoreStats();
        }

        public static void SubmitLeaderboardScore(string leaderboardName, int score)
        {
            if (!IsAvailable || string.IsNullOrEmpty(leaderboardName))
                return;

            // SteamUserStats.UploadLeaderboardScore(...)
        }
    }
}
