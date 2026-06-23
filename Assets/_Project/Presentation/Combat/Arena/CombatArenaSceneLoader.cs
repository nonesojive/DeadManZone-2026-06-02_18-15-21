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

        public IEnumerator UnloadAsync()
        {
            if (!IsLoaded)
                yield break;

            IsLoaded = false;
            CombatArenaUiController.ExitArenaMode(runSceneController?.BuildPanelTransform);
            GetComponent<CombatArenaPresenter>()?.OnArenaUnloaded();

            string sceneToUnload = SceneManager.GetSceneByName(GameScenes.CombatArena2D).isLoaded
                ? GameScenes.CombatArena2D
                : GameScenes.CombatArena;
            var op = SceneManager.UnloadSceneAsync(sceneToUnload);
            while (op != null && !op.isDone)
                yield return null;

            runSceneController?.RefreshCombatPresentation();
        }
    }
}
