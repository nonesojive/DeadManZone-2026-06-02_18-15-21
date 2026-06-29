using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Zone tint colors + checker shading for the 2D Top Troops battlefield tiles.</summary>
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

        /// <summary>Zone color for a cell with checkerboard shading applied.</summary>
        public Color ResolveCellColor(BattlefieldLayout layout, int x, int y)
        {
            Color zone;
            if (layout.IsPlayerHalf(x))
                zone = PlayerZoneColor;
            else if (layout.IsNeutralColumn(x))
                zone = NeutralZoneColor;
            else
                zone = EnemyZoneColor;

            return ApplyCheckerShade(zone, GetCheckerShade(x, y));
        }

        public static float GetCheckerShade(int x, int y) => (x + y) % 2 == 0 ? 1f : 0.86f;

        public static Color ApplyCheckerShade(Color zone, float shade) => zone * shade;
    }
}
