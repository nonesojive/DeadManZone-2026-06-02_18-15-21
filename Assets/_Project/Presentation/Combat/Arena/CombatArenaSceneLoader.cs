using System.Collections;
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
            if (!SceneManager.GetSceneByName(GameScenes.CombatArena).isLoaded)
                IsLoaded = false;

            if (IsLoaded)
                yield break;

            var op = SceneManager.LoadSceneAsync(GameScenes.CombatArena, LoadSceneMode.Additive);
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

            var op = SceneManager.UnloadSceneAsync(GameScenes.CombatArena);
            while (op != null && !op.isDone)
                yield return null;

            runSceneController?.RefreshCombatPresentation();
        }
    }
}
