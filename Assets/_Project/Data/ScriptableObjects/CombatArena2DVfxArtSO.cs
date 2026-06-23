using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(
        fileName = "CombatArena2DVfxArt",
        menuName = "DeadManZone/Combat Arena 2D VFX Art")]
    public sealed class CombatArena2DVfxArtSO : ScriptableObject
    {
        public Sprite rifleImpactStrip;
        public Sprite explosionSmallStrip;
        public Sprite deathPuffStrip;

        public bool HasAny =>
            rifleImpactStrip != null || explosionSmallStrip != null || deathPuffStrip != null;
    }
}
