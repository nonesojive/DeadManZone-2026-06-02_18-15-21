using System.Collections.Generic;
using DeadManZone.Core.Board;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public static class CombatArenaGridMeshBuilder
    {
        public static Mesh Build(
            BattlefieldLayout layout,
            float cellWidth,
            float cellDepth,
            Color lightCell,
            Color darkCell,
            float cellInset,
            float yOffset,
            out Material[] materials)
        {
            int width = layout.TotalWidth;
            int height = layout.Height;

            var lightVertices = new List<Vector3>();
            var lightTriangles = new List<int>();
            var darkVertices = new List<Vector3>();
            var darkTriangles = new List<int>();

            float insetX = Mathf.Clamp(cellInset, 0f, cellWidth * 0.2f);
            float insetZ = Mathf.Clamp(cellInset, 0f, cellDepth * 0.2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float centerX = (x + 0.5f - width * 0.5f) * cellWidth;
                    float centerZ = (height * 0.5f - y - 0.5f) * cellDepth;
                    float halfW = cellWidth * 0.5f - insetX;
                    float halfD = cellDepth * 0.5f - insetZ;

                    bool isLight = (x + y) % 2 == 0;
                    var vertices = isLight ? lightVertices : darkVertices;
                    var triangles = isLight ? lightTriangles : darkTriangles;
                    int baseIndex = vertices.Count;

                    vertices.Add(new Vector3(centerX - halfW, yOffset, centerZ - halfD));
                    vertices.Add(new Vector3(centerX + halfW, yOffset, centerZ - halfD));
                    vertices.Add(new Vector3(centerX + halfW, yOffset, centerZ + halfD));
                    vertices.Add(new Vector3(centerX - halfW, yOffset, centerZ + halfD));

                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }

            var mesh = new Mesh { name = "CombatArenaGrid" };
            mesh.subMeshCount = 2;
            mesh.SetVertices(CombineVertices(lightVertices, darkVertices));
            mesh.SetTriangles(lightTriangles, 0);
            mesh.SetTriangles(OffsetTriangles(darkTriangles, lightVertices.Count), 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            materials = new[]
            {
                CreateCellMaterial(lightCell),
                CreateCellMaterial(darkCell)
            };

            return mesh;
        }

        private static List<Vector3> CombineVertices(List<Vector3> light, List<Vector3> dark)
        {
            var combined = new List<Vector3>(light.Count + dark.Count);
            combined.AddRange(light);
            combined.AddRange(dark);
            return combined;
        }

        private static List<int> OffsetTriangles(List<int> triangles, int vertexOffset)
        {
            if (vertexOffset == 0)
                return triangles;

            var offset = new List<int>(triangles.Count);
            foreach (int index in triangles)
                offset.Add(index + vertexOffset);
            return offset;
        }

        private static Material CreateCellMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var material = new Material(shader)
            {
                name = $"CombatArenaGridCell_{color}",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            return material;
        }
    }
}
