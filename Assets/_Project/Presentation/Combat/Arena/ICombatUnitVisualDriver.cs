using System;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public interface ICombatUnitVisualDriver
    {
        void Bind(Animator animator, CombatArenaAnimationSetSO animationSet);
        void SetWalking(bool walking);
        void PlayAttack(CombatAttackPresentationProfile profile, Action onMuzzleFrame, Action onImpactFrame);
        void PlayDeath(Action onComplete);
        void Clear();
        Vector3 GetMuzzleWorldPosition();
    }
}
