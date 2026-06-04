using System;
using DeadManZone.Core.Run;
using UnityEngine;

namespace DeadManZone.Game
{
    /// <summary>Persists the active run on pause/quit until RunManager owns save triggers.</summary>
    public sealed class RunSaveBootstrap : MonoBehaviour
    {
        public static RunSaveBootstrap Instance { get; private set; }

        public static Func<RunState> GetActiveRunState { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                TryAutoSave();
        }

        private void OnApplicationQuit()
        {
            TryAutoSave();
        }

        private static void TryAutoSave()
        {
            var state = GetActiveRunState?.Invoke();
            if (state != null)
                SaveManager.Save(state);
        }
    }
}
