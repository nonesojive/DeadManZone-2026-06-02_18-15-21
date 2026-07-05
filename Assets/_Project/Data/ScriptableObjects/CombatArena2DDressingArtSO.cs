using UnityEngine;

namespace DeadManZone.Data
{
    /// <summary>Source sheets for battlefield dressing props (WW1 trench tilesets).
    /// Prop rects live in code; this only carries the texture references so the
    /// presentation layer can load them from Resources at runtime.</summary>
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena 2D Dressing Art")]
    public sealed class CombatArena2DDressingArtSO : ScriptableObject
    {
        [Tooltip("WW1 trench sheet with sandbag clusters (bottom-left region).")]
        public Texture2D sandbagSheet;

        [Tooltip("WW1 trench sheet with barbed wire fences and barrels.")]
        public Texture2D wireSheet;

        [Tooltip("WW1 ruins sheet with shell craters, rubble, and wire obstacles.")]
        public Texture2D ruinsSheet;
    }
}
