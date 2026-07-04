using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Combat VFX contract shared by 3D and 2D arena backends.</summary>
    public interface ICombatArenaVfxPresenter
    {
        void PlayRifleMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld);
        void PlayCannonMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld);
        void PlayImpact(Vector3 targetWorld, int damageAmount);
        void PlayExplosion(Vector3 targetWorld, int damageAmount);
        void PlayDeath(Vector3 worldPosition);
        void PlayDamage(Vector3 worldPosition, int amount);

        /// <summary>Environmental damage (gas, hazards): feedback without weapon cues.</summary>
        void PlayEnvironmentalDamage(Vector3 targetWorld, int damageAmount);
    }
}
