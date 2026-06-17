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
            new Color(0.42f, 0.58f, 0.36f),
            new Color(0.38f, 0.52f, 0.32f),
            new Color(0.36f, 0.48f, 0.30f));

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
