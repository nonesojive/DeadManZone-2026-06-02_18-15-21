using System;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Seam between <see cref="CombatUnitActor"/> (movement/replay driving) and a
    /// unit's rendering backend. Exactly the members the actor and presenter consume — the
    /// 2D sprite pipeline implements it today; the 3D toon-ink visual is the next implementer.</summary>
    public interface ICombatUnitVisual
    {
        /// <summary>True while a one-shot (attack/death) should suppress locomotion.</summary>
        bool BlocksLocomotion { get; }

        /// <summary>Approx world height of the rendered figure (feet→head) for UI/VFX anchoring.</summary>
        float VisualHeight { get; }

        /// <summary>Seconds the death presentation takes before the corpse can be pooled;
        /// the presenter times death VFX/audio to this.</summary>
        float DeathSeconds { get; }

        /// <summary>True when the visual presents unit HP itself (3D ring fill); the actor
        /// then skips the floating overhead bar and routes HP through
        /// <see cref="SetHealthFraction"/> instead.</summary>
        bool DisplaysHealth { get; }

        /// <summary>Latest replayed HP fraction (0..1) for this unit; no-op for backends
        /// that leave HP to the overhead bar.</summary>
        void SetHealthFraction(float fraction);

        void SetWalking(bool walking);
        void FaceDirection(Vector3 worldDirection);
        void UpdateSortAndBob(Vector3 worldPosition);
        void PlayAttack(
            CombatAttackPresentationProfile profile,
            Vector3 targetWorld,
            Action<Vector3> onMuzzle,
            Action onImpact);
        void PlayHurt();
        void PlayDeath(Action onComplete);
        void Clear();
    }
}
