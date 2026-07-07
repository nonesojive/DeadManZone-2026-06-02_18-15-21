using System.Collections;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Plays a horizontal sprite strip once on a transient SpriteRenderer.</summary>
    internal static class CombatArena2DVfxSpriteAnim
    {
        public static Coroutine Play(
            MonoBehaviour host,
            Sprite[] frames,
            Vector3 worldPosition,
            float scale,
            float durationSeconds,
            int sortOrder)
        {
            if (host == null || frames == null || frames.Length == 0)
                return null;

            return host.StartCoroutine(PlayRoutine(frames, worldPosition, scale, durationSeconds, sortOrder));
        }

        private static IEnumerator PlayRoutine(
            Sprite[] frames,
            Vector3 worldPosition,
            float scale,
            float durationSeconds,
            int sortOrder)
        {
            var camera = CombatArenaBootstrap.Instance?.ArenaCamera;
            var go = CombatArena2DSpriteQuad.CreateBillboard(
                frames[0],
                Color.white,
                worldPosition,
                scale,
                CombatArena2DSortOrder.RenderQueueFromWorldZ(worldPosition.z, 50),
                camera);
            if (go == null)
                yield break;

            go.name = "Combat2DVfx";
            var meshFilter = go.GetComponentInChildren<MeshFilter>();
            var renderer = go.GetComponentInChildren<Renderer>();

            // Every frame of a strip shares the same source texture; only the mesh UVs change.
            // Build one material for the whole strip (not one per frame — that leaked a material
            // on every impact/explosion, thousands per fight) and destroy it when the VFX ends.
            Material material = null;
            if (renderer != null)
            {
                material = CombatArena2DSpriteMaterial.CreateSpriteAdditive(
                    frames[0],
                    CombatArena2DSortOrder.RenderQueueFromWorldZ(worldPosition.z, 50));
                renderer.sharedMaterial = material;
            }

            float frameDuration = durationSeconds / frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                if (meshFilter != null)
                    CombatArena2DSpriteMesh.Apply(meshFilter, frames[i]);

                if (frameDuration > 0f)
                    yield return new WaitForSeconds(frameDuration);
            }

            if (material != null)
                Object.Destroy(material);
            Object.Destroy(go);
        }
    }
}
