using UnityEngine;

namespace DeadManZone.Presentation.Settings
{
    /// <summary>Player-facing graphics options, persisted via PlayerPrefs. Read by the combat
    /// post-processing so players who dislike (or can't afford) the grade can turn it off.</summary>
    public static class GraphicsSettings
    {
        private const string PostProcessingKey = "PostProcessingEnabled";

        public static bool PostProcessingEnabled
        {
            get => PlayerPrefs.GetInt(PostProcessingKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(PostProcessingKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
