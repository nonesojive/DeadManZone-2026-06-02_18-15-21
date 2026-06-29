using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    public static class CombatArenaPresentationMode
    {
        // Project is locked to the 2D (Top Troops) combat presentation; the legacy
        // 3D arena backend has been removed, so this is always true.
        public static bool IsTopTroops2D(CombatArenaConfigSO config) => true;
    }
}
