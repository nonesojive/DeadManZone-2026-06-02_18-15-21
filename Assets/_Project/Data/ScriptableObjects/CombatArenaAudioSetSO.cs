using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Audio Set")]
    public sealed class CombatArenaAudioSetSO : ScriptableObject
    {
        [Header("Weapon fire")]
        public AudioClip rifleShot;
        public AudioClip cannonShot;

        [Header("Impacts")]
        public AudioClip bulletImpact;
        public AudioClip explosion;

        [Header("Unit death")]
        public AudioClip unitDeath;
    }
}
