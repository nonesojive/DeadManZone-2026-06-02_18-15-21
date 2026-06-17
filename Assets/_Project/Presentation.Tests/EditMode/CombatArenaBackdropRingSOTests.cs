using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaBackdropRingSOTests
    {
        [Test]
        public void ResolvePrefabPath_WrapsCatalogIndex()
        {
            var ring = ScriptableObject.CreateInstance<CombatArenaBackdropRingSO>();
            ring.prefabPaths = new[] { "a.prefab", "b.prefab" };

            try
            {
                Assert.AreEqual("a.prefab", ring.ResolvePrefabPath(0));
                Assert.AreEqual("b.prefab", ring.ResolvePrefabPath(1));
                Assert.AreEqual("a.prefab", ring.ResolvePrefabPath(2));
            }
            finally
            {
                Object.DestroyImmediate(ring);
            }
        }

        [Test]
        public void ResolvePrefabPath_ReturnsNullWhenCatalogEmpty()
        {
            var ring = ScriptableObject.CreateInstance<CombatArenaBackdropRingSO>();

            try
            {
                Assert.IsNull(ring.ResolvePrefabPath(0));
            }
            finally
            {
                Object.DestroyImmediate(ring);
            }
        }
    }
}
