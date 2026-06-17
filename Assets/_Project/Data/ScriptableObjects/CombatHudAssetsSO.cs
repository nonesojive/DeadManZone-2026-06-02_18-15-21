using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat HUD Assets")]
    public sealed class CombatHudAssetsSO : ScriptableObject
    {
        [Header("Army health bars")]
        public GameObject armyHealthBarPrefab;

        [Header("Combat chrome")]
        public Sprite combatBackgroundSprite;

        [Header("Audio (optional — wired by editor bootstrap)")]
        public AudioClip rifleShotClip;
        public AudioClip cannonShotClip;
        public AudioClip impactClip;
        public AudioClip explosionClip;
        public AudioClip deathClip;
    }
}
