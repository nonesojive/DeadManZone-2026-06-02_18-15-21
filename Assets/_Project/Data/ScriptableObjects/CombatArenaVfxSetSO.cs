using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena VFX Set")]
    public sealed class CombatArenaVfxSetSO : ScriptableObject
    {
        [Header("Rifle / ballistic")]
        public ParticleSystem rifleMuzzle;
        public ParticleSystem rifleMuzzleSmoke;
        public ParticleSystem bulletTracer;
        public ParticleSystem rifleImpact;

        [Header("Heavy / explosive")]
        public ParticleSystem cannonShot;
        public ParticleSystem explosionSmall;
        public ParticleSystem explosionLarge;

        [Header("Death")]
        public ParticleSystem deathBurst;
        public ParticleSystem deathSmoke;
    }
}
