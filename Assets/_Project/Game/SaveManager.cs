using System;
using System.IO;
using DeadManZone.Core.Run;
using UnityEngine;

namespace DeadManZone.Game
{
    public static class SaveManager
    {
        private const string FileName = "run_save.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave() => File.Exists(SavePath);

        public static void Save(RunState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var json = RunSaveSerializer.ToJson(state);
            File.WriteAllText(SavePath, json);
        }

        public static RunState Load()
        {
            if (!HasSave())
                return null;

            try
            {
                return RunSaveSerializer.FromJson(File.ReadAllText(SavePath));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load save: {ex.Message}");
                return null;
            }
        }

        public static void DeleteSave()
        {
            if (HasSave())
                File.Delete(SavePath);
        }
    }
}
