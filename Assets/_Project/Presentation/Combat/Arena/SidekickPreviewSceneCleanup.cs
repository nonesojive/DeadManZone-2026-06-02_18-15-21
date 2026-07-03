#if UNITY_EDITOR
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

        private void Update()
        {
            if (!CombatArenaSession.IsActive)
                return;

            DestroyLeakedPreviewIfPresent();
        }

        public static void DestroyLeakedPreviewIfPresent()
        {
            var leaked = GameObject.Find(SidekickPreviewName);
            if (leaked == null || leaked.transform.parent != null)
                return;

            // Real combat units are CombatUnitActor children under UnitsRoot.
            if (leaked.GetComponent<CombatUnitActor>() != null
                || leaked.GetComponentInParent<CombatUnitActor>() != null)
                return;

            Object.Destroy(leaked);
        }
    }
}
#endif
