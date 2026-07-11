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

        public bool IsLoaded { get; private set; }

        /// <summary>For scenes that embed the arena directly (Combat3D demo) instead of
        /// additively loading the 2D arena scene: marks the session active so
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
            // flag can desync while the additive scene lingers behind the shop.
            bool sceneLoaded = SceneManager.GetSceneByName(GameScenes.CombatArena2D).isLoaded;
            if (!IsLoaded && !sceneLoaded)
                yield break;

            IsLoaded = false;
            CombatArenaUiController.ExitArenaMode(runSceneController?.BuildPanelTransform);
            GetComponent<CombatArenaPresenter>()?.OnArenaUnloaded();

            if (sceneLoaded)
            {
                var op = SceneManager.UnloadSceneAsync(GameScenes.CombatArena2D);
                while (op != null && !op.isDone)
                    yield return null;
            }

            runSceneController?.RefreshCombatPresentation();
        }
    }
}
