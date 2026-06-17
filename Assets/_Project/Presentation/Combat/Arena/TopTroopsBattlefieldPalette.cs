using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public readonly struct TopTroopsBattlefieldPalette
    {
        public Color PlayerZoneColor { get; }
        public Color NeutralZoneColor { get; }
        public Color EnemyZoneColor { get; }

        public TopTroopsBattlefieldPalette(Color playerZone, Color neutralZone, Color enemyZone)
        {
            PlayerZoneColor = playerZone;
            NeutralZoneColor = neutralZone;
            EnemyZoneColor = enemyZone;
        }

        public static TopTroopsBattlefieldPalette Default => new(
            new Color(0.54f, 0.44f, 0.30f),
            new Color(0.46f, 0.42f, 0.36f),
            new Color(0.42f, 0.34f, 0.30f));

        public static TopTroopsBattlefieldPalette FromConfig(CombatArenaConfigSO config)
        {
            if (config == null)
                return Default;

            return new TopTroopsBattlefieldPalette(
                config.topTroopsPlayerZoneColor,
                config.topTroopsNeutralZoneColor,
                config.topTroopsEnemyZoneColor);
        }
    }
}
