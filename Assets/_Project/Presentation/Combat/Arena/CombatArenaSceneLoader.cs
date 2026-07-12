using System.Collections;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaSceneLoader : MonoBehaviour
    {
        [SerializeField] private RunSceneController runSceneController;

        // Which arena scene this loader actually loaded (2D or 3D); unload must target the
        // same scene even if the config asset changes between load and unload.
        private string _loadedSceneName;
        private string _previousActiveSceneName;

        public bool IsLoaded { get; private set; }

        /// <summary>For scenes that embed the arena directly (Combat3D demo) instead of
        /// additively loading an arena scene: marks the session active so
        /// <see cref="CombatArenaPresenter"/> accepts director replay events.</summary>
        public void MarkEmbeddedArenaLoaded() => IsLoaded = true;

        private void Awake()
        {
            if (runSceneController == null)
                runSceneController = FindFirstObjectByType<RunSceneController>();

            CombatArenaSession.Bind(this);
        }

        private void OnDestroy() => CombatArenaSession.Unbind(this);

        public IEnumerator LoadAsync()
        {
            var config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");
            string sceneName = GameScenes.ResolveCombatArenaScene(config);

            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                IsLoaded = false;

            if (IsLoaded)
                yield break;

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (op != null && !op.isDone)
                yield return null;

            IsLoaded = true;
            _loadedSceneName = sceneName;

            // The 3D arena scene owns its lighting/fog (RenderSettings are per-scene and
            // only the ACTIVE scene's apply). Gated to the 3D scene so the 2D path keeps
            // its exact old behavior. Restored on unload.
            if (sceneName == GameScenes.CombatArena3D)
            {
                var arenaScene = SceneManager.GetSceneByName(sceneName);
                if (arenaScene.IsValid() && arenaScene.isLoaded)
                {
                    _previousActiveSceneName = SceneManager.GetActiveScene().name;
                    SceneManager.SetActiveScene(arenaScene);
                }
            }

            if (runSceneController == null)
                runSceneController = FindFirstObjectByType<RunSceneController>();

            runSceneController?.RefreshCombatPresentation();

            var arenaCamera = CombatArenaBootstrap.Instance != null
                ? CombatArenaBootstrap.Instance.ArenaCamera
                : null;

            CombatArenaUiController.EnterArenaMode(runSceneController?.BuildPanelTransform, arenaCamera);
        }

        /// <summary>Fire-and-forget unload used by always-alive callers (e.g. the run scene
        /// controller) to guarantee the arena is gone when we return to the shop — including
        /// the defeat path, which never routes through the presenter's Build safety net.</summary>
        public void RequestUnload()
        {
            if (isActiveAndEnabled)
                StartCoroutine(UnloadAsync());
        }

        public IEnumerator UnloadAsync()
        {
            // Trust the actual scene state, not just the flag: on the defeat→new-run path the
            // flag can desync while the additive scene lingers behind the shop. When the flag
            // desynced before this loader ever loaded, fall back to the config-resolved name.
            string sceneName = _loadedSceneName ?? GameScenes.ResolveCombatArenaScene(
                Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig"));
            bool sceneLoaded = SceneManager.GetSceneByName(sceneName).isLoaded;
            if (!IsLoaded && !sceneLoaded)
                yield break;

            IsLoaded = false;
            _loadedSceneName = null;
            CombatArenaUiController.ExitArenaMode(runSceneController?.BuildPanelTransform);
            GetComponent<CombatArenaPresenter>()?.OnArenaUnloaded();

            // Hand active-scene (and its RenderSettings) back before the arena goes away.
            if (_previousActiveSceneName != null)
            {
                var previous = SceneManager.GetSceneByName(_previousActiveSceneName);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                _previousActiveSceneName = null;
            }

            if (sceneLoaded)
            {
                var op = SceneManager.UnloadSceneAsync(sceneName);
                while (op != null && !op.isDone)
                    yield return null;
            }

            runSceneController?.RefreshCombatPresentation();
        }
    }
}
