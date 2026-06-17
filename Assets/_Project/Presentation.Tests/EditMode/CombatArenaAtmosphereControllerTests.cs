using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaAtmosphereControllerTests
    {
        [Test]
        public void Ensure_CreatesControllerChildOnArenaRoot()
        {
            var root = new GameObject("ArenaRoot");

            try
            {
                var controller = CombatArenaAtmosphereController.Ensure(root.transform);

                Assert.NotNull(controller);
                Assert.AreEqual(root.transform, controller.transform.parent);
                Assert.AreEqual("CombatArenaAtmosphere", controller.gameObject.name);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Ensure_ReusesExistingController()
        {
            var root = new GameObject("ArenaRoot");

            try
            {
                var first = CombatArenaAtmosphereController.Ensure(root.transform);
                var second = CombatArenaAtmosphereController.Ensure(root.transform);

                Assert.AreSame(first, second);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
