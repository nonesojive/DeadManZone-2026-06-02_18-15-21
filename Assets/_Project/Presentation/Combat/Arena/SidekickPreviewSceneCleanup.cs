#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Synty Sidekick's ModularCharacterWindow spawns a scene-root "Combined Character" preview when play mode starts.
    /// It has no gameplay animator and sits at the origin — looks like a frozen T-pose on the battlefield.
    /// ponytail: name-based cull for Sidekick preview only; close Sidekick window to stop respawns at source.
    /// </summary>
    public sealed class SidekickPreviewSceneCleanup : MonoBehaviour
    {
        private const string SidekickPreviewName = "Combined Character";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var host = new GameObject(nameof(SidekickPreviewSceneCleanup));
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);
            host.AddComponent<SidekickPreviewSceneCleanup>();
        }

        private void Start() => StartCoroutine(CullForSeconds(8f));

        /// <summary>Sidekick may spawn the preview a few frames after play mode via editor callback queue.</summary>
        private IEnumerator CullForSeconds(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                DestroyLeakedPreviewIfPresent();
                yield return null;
            }
        }

        public static void DestroyLeakedPreviewIfPresent()
        {
            var leaked = GameObject.Find(SidekickPreviewName);
            if (leaked == null || leaked.transform.parent != null)
                return;

            // Real combat units live under CombatArena/UnitsRoot with AC_CombatArena_* controllers.
            var animator = leaked.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                return;

            Object.Destroy(leaked);
        }
    }
}
#endif
