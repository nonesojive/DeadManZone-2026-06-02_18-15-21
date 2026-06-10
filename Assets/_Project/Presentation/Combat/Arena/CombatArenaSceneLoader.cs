using System.Collections;
using DeadManZone.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaSceneLoader : MonoBehaviour
    {
        public bool IsLoaded { get; private set; }

        public IEnumerator LoadAsync()
        {
            if (IsLoaded)
                yield break;

            var op = SceneManager.LoadSceneAsync(GameScenes.CombatArena, LoadSceneMode.Additive);
            while (op != null && !op.isDone)
                yield return null;

            IsLoaded = true;
            CombatPresentationMode.ArenaActive = true;
        }

        public IEnumerator UnloadAsync()
        {
            if (!IsLoaded)
                yield break;

            CombatPresentationMode.ArenaActive = false;
            var op = SceneManager.UnloadSceneAsync(GameScenes.CombatArena);
            while (op != null && !op.isDone)
                yield return null;

            IsLoaded = false;
        }
    }
}
