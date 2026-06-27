using UnityEngine;

namespace DeadManZone.Data
{
    /// <summary>Horizontal sprite strips for Top Troops 2D unit states.</summary>
    [CreateAssetMenu(menuName = "DeadManZone/Combat Unit 2D Animation Set")]
    public sealed class CombatUnit2DAnimationSetSO : ScriptableObject
    {
        [Header("Locomotion")]
        public CombatUnit2DStrip idle;
        public CombatUnit2DStrip walk;
        public CombatUnit2DStrip run;

        [Header("Combat")]
        public CombatUnit2DStrip hurt;
        public CombatUnit2DStrip hitReact;
        public CombatUnit2DStrip shoot;
        public CombatUnit2DStrip die;

        public bool HasAny => idle.IsValid || walk.IsValid || run.IsValid
            || hurt.IsValid || hitReact.IsValid || shoot.IsValid || die.IsValid;
    }

    [System.Serializable]
    public struct CombatUnit2DStrip
    {
        public Sprite sheet;
        [Min(1)] public int frameCount;
        [Min(1f)] public float framesPerSecond;
        public bool loop;

        public bool IsValid => sheet != null && frameCount > 0;

        public float DurationSeconds => frameCount > 0 && framesPerSecond > 0f
            ? frameCount / framesPerSecond
            : 0f;
    }
}
