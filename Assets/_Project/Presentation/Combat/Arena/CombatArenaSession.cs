namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Read-only view of whether the additive combat arena scene is loaded.
    /// Only <see cref="CombatArenaSceneLoader"/> mutates the bound loader.
    /// </summary>
    public static class CombatArenaSession
    {
        private static CombatArenaSceneLoader _loader;

        public static bool IsActive => _loader != null && _loader.IsLoaded;

        internal static void Bind(CombatArenaSceneLoader loader) => _loader = loader;

        internal static void Unbind(CombatArenaSceneLoader loader)
        {
            if (_loader == loader)
                _loader = null;
        }

        public static void ResetForTests() => _loader = null;
    }
}
