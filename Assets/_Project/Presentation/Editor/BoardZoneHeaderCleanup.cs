using DeadManZone.Presentation.Board;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class BoardZoneHeaderCleanup
    {
        private const string BoardAreaPath = "Canvas/RunScene/BuildPanel/MainRow/BoardArea";

        [MenuItem("DeadManZone/Remove Legacy Board Zone Headers")]
        public static void RemoveFromActiveScene()
        {
            var board = GameObject.Find(BoardAreaPath);
            BoardZoneStripLayout.RemoveLegacyHeaders(board != null ? board.transform : null);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Removed legacy REAR/SUPPORT/FRONT headers above the board.");
        }

        [MenuItem("DeadManZone/Remove Legacy Board Zone Headers", true)]
        private static bool RemoveFromActiveSceneValidate() =>
            !Application.isPlaying;
    }
}
