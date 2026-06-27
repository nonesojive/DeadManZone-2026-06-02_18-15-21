using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Unit quad with sprite atlas UVs (material scale/offset is unreliable for cutout sprites).</summary>
    internal static class CombatArena2DSpriteMesh
    {
        private static readonly Vector3[] UnitVertices =
        {
            new(-0.5f, -0.5f, 0f),
            new(0.5f, -0.5f, 0f),
            new(-0.5f, 0.5f, 0f),
            new(0.5f, 0.5f, 0f)
        };

        private static readonly int[] UnitTriangles = { 0, 2, 1, 2, 3, 1 };

        public static void Apply(MeshFilter meshFilter, Sprite sprite)
        {
            if (meshFilter == null || sprite == null)
                return;

            var mesh = new Mesh
            {
                name = $"Combat2DSprite_{sprite.name}",
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.vertices = UnitVertices;
            mesh.uv = ResolveUnitUvs(sprite);
            mesh.triangles = UnitTriangles;
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
        }

        /// <summary>Repoint an existing quad mesh at another frame sprite (cheap; no mesh realloc).</summary>
        public static void UpdateUvs(MeshFilter meshFilter, Sprite sprite)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null || sprite == null)
                return;

            meshFilter.sharedMesh.uv = ResolveUnitUvs(sprite);
        }

        /// <summary>Four corner UVs for a unit quad; tight sprite meshes can expose dozens of UVs.</summary>
        internal static Vector2[] ResolveUnitUvs(Sprite sprite)
        {
            var texture = sprite.texture;
            var rect = sprite.textureRect;
            float u0 = rect.x / texture.width;
            float v0 = rect.y / texture.height;
            float u1 = (rect.x + rect.width) / texture.width;
            float v1 = (rect.y + rect.height) / texture.height;

            return new[]
            {
                new Vector2(u0, v0),
                new Vector2(u1, v0),
                new Vector2(u0, v1),
                new Vector2(u1, v1)
            };
        }
    }
}
