using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// No-op driver for static mesh vehicles and buildings in the combat arena.
    /// </summary>
    public sealed class StaticMeshVisualDriver : ICombatUnitVisualDriver
    {
        public void Bind(Animator animator) { }

        public void SetWalking(bool walking) { }

        public void PlayAttack() { }

        public void Clear() { }
    }
}
