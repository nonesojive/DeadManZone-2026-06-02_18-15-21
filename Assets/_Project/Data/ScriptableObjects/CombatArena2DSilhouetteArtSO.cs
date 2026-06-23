using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(
        fileName = "CombatArena2DSilhouetteArt",
        menuName = "DeadManZone/Combat Arena 2D Silhouette Art")]
    public sealed class CombatArena2DSilhouetteArtSO : ScriptableObject
    {
        public Sprite assault;
        public Sprite ranged;
        public Sprite artillery;
        public Sprite vehicle;
        public Sprite generic;

        public bool HasAny =>
            assault != null || ranged != null || artillery != null || vehicle != null || generic != null;
    }
}
