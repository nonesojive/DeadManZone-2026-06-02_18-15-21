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

            float frameDuration = durationSeconds / frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                if (meshFilter != null)
                    CombatArena2DSpriteMesh.Apply(meshFilter, frames[i]);
                if (renderer != null)
                    renderer.sharedMaterial = CombatArena2DSpriteMaterial.CreateSprite(
                        frames[i],
                        Color.white,
                        CombatArena2DSortOrder.RenderQueueFromWorldZ(worldPosition.z, 50));

                if (frameDuration > 0f)
                    yield return new WaitForSeconds(frameDuration);
            }

            Object.Destroy(go);
        }
    }
}
