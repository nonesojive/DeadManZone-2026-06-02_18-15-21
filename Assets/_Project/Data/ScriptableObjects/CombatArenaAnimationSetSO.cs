using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Animation Set")]
    public sealed class CombatArenaAnimationSetSO : ScriptableObject
    {
        [Header("Kevin Iglesias — shoot")]
        public AnimationClip rifleShoot;
        public AnimationClip grenadeThrow;

        [Header("Kevin Iglesias — death")]
        public AnimationClip death01;
        public AnimationClip death02;
        public AnimationClip death03;

        [Header("Synty Sidekick fallback death")]
        public AnimationClip sidekickDeathForward;
    }
}
