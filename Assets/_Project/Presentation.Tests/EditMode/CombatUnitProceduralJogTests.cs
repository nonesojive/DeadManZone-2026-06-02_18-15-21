using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatUnitProceduralJogTests
    {
        [Test]
        public void BobY_Walking_UsesLargerAmplitudeThanIdle()
        {
            float walk = CombatUnitProceduralJog.EvaluateBobY(Mathf.PI * 0.5f, walking: true);
            float idle = CombatUnitProceduralJog.EvaluateBobY(Mathf.PI * 0.5f, walking: false);
            Assert.Greater(Mathf.Abs(walk), Mathf.Abs(idle));
        }

        [Test]
        public void SquadScale_Walking_SquashesVerticallyAtPeak()
        {
            var scale = CombatUnitProceduralJog.EvaluateSquadScale(Mathf.PI * 0.5f, walking: true);
            Assert.Greater(scale.x, 1f);
            Assert.Less(scale.y, 1f);
            Assert.AreEqual(1f, scale.z, 0.001f);
        }

        [Test]
        public void SquadScale_Idle_StaysUnity()
        {
            Assert.AreEqual(Vector3.one, CombatUnitProceduralJog.EvaluateSquadScale(1.2f, walking: false));
        }

        [Test]
        public void Lean_Walking_NonZeroAtMidCycle()
        {
            float lean = CombatUnitProceduralJog.EvaluateLeanDegrees(Mathf.PI * 0.5f, walking: true);
            Assert.Greater(Mathf.Abs(lean), 0.001f);
        }

        [Test]
        public void Lean_Idle_IsZero()
        {
            Assert.AreEqual(0f, CombatUnitProceduralJog.EvaluateLeanDegrees(1.2f, walking: false), 0.001f);
        }

        [Test]
        public void PhaseOffset_StaggersSoldiers()
        {
            Assert.AreNotEqual(
                CombatUnitProceduralJog.PhaseOffset(0),
                CombatUnitProceduralJog.PhaseOffset(1));
        }
    }
}
