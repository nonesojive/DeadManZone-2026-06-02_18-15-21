using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public interface ICombatUnitVisualDriver
    {
        void Bind(Animator animator);
        void SetWalking(bool walking);
        void PlayAttack();
        void Clear();
    }
}
