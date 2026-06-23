using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena 2D Environment Art")]
    public class CombatArena2DEnvironmentArtSO : ScriptableObject
    {
        [Header("Grid")]
        public Sprite gridCellLight;
        public Sprite gridCellDark;
        public Sprite gridBackdrop;

        [Header("Atmosphere")]
        public Sprite skyGradient;

        [Header("Shadows")]
        public Sprite shadowUnit;
        public Sprite shadowBuilding;
    }
}
