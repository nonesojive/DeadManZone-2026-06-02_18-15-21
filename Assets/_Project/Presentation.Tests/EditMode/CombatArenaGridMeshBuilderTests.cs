using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaGridMeshBuilderTests
    {
        [Test]
        public void Build_CreatesOneQuadPerCell()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var light = new Color(0.55f, 0.42f, 0.30f);
            var dark = new Color(0.40f, 0.30f, 0.22f);

            var mesh = CombatArenaGridMeshBuilder.Build(layout, 1.8f, 1.8f, light, dark, 0.08f, 0.02f, out var materials);

            try
            {
                int expectedCells = layout.TotalWidth * layout.Height;
                Assert.AreEqual(expectedCells * 4, mesh.vertexCount);
                Assert.AreEqual(2, mesh.subMeshCount);
                Assert.AreEqual(2, materials.Length);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                foreach (var material in materials)
                    Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void Build_CheckerboardColorsAlternate()
        {
            var layout = new BattlefieldLayout(2, 1, 2, 2);
            var light = Color.red;
            var dark = Color.blue;

            var mesh = CombatArenaGridMeshBuilder.Build(layout, 1f, 1f, light, dark, 0.05f, 0f, out var materials);

            try
            {
                Assert.That(ReadMaterialColor(materials[0]), Is.EqualTo(light));
                Assert.That(ReadMaterialColor(materials[1]), Is.EqualTo(dark));
                Assert.AreEqual(10, mesh.GetTriangles(0).Length / 3);
                Assert.AreEqual(10, mesh.GetTriangles(1).Length / 3);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                foreach (var material in materials)
                    Object.DestroyImmediate(material);
            }
        }

        private static Color ReadMaterialColor(Material material)
        {
            if (material != null && material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");

            return material != null ? material.color : Color.white;
        }
    }
}
