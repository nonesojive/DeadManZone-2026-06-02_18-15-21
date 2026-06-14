using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Drives Synty AnimationBaseLocomotion (AC_Polygon_Masculine) for arena units.
    /// </summary>
    public sealed class SyntyLocomotionVisualDriver : ICombatUnitVisualDriver
    {
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int CurrentGait = Animator.StringToHash("CurrentGait");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

        private const float WalkSpeed = 1.5f;
        private const int WalkGait = 1;

        private Animator _animator;

        public void Bind(Animator animator)
        {
            _animator = animator;
            if (_animator == null)
                return;

            _animator.SetBool(IsGrounded, true);
            _animator.SetFloat(MoveSpeed, 0f);
            _animator.SetBool(IsWalking, false);
            _animator.SetInteger(CurrentGait, 0);
        }

        public void SetWalking(bool walking)
        {
            if (_animator == null)
                return;

            _animator.SetBool(IsWalking, walking);
            _animator.SetFloat(MoveSpeed, walking ? WalkSpeed : 0f);
            _animator.SetInteger(CurrentGait, walking ? WalkGait : 0);
        }

        public void PlayAttack()
        {
            SetWalking(false);
        }

        public void Clear()
        {
            _animator = null;
        }
    }
}
