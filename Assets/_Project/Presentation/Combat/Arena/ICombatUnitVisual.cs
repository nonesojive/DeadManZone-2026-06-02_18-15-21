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

        /// <summary>Latest replayed Morale fraction (0..1). Only invoked for units that
        /// can break (Definition.MaxMorale &gt; 0) — backends lazily build the morale
        /// strip on the first call, so morale-immune units show nothing new (ADR-0005).</summary>
        void SetMoraleFraction(float fraction);

        /// <summary>True when a broken unit runs for its own board edge; vehicles return
        /// false and slump-abandon in place inside <see cref="PlayRoutExit"/>.</summary>
        bool FleesWhenBroken { get; }

        /// <summary>Rout exit (ADR-0005): the ESCAPE presentation — a softer dissolve with
        /// no die clip and no death VFX/audio hooks. Humanoids keep their walk cycle while
        /// the actor marches them off-field; vehicles settle in place as abandoned.</summary>
        void PlayRoutExit(Action onComplete);

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
