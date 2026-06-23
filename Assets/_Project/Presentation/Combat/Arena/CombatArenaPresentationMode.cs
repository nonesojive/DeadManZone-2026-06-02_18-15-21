using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    public static class CombatArenaPresentationMode
    {
        public static bool IsTopTroops2D(CombatArenaConfigSO config) =>
            config != null && config.visualMode == CombatArenaVisualMode.TopTroops2D;
    }
}
