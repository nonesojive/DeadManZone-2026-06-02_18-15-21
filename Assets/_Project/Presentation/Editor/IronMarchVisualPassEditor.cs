using DeadManZone.Data.Editor;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>One-shot IronMarch themed visual pass: board tiles + piece icons/cells.</summary>
    public static class IronMarchVisualPassEditor
    {
        [MenuItem(DeadManZoneEditorMenus.Art + "Run IronMarch Themed Visual Pass")]
        public static void RunVisualPass()
        {
            ThemedBoardTerrainEditor.ImportThemedBoardTiles();
            var icons = IronMarchPieceArtImporter.ImportAll();
            AssetDatabase.Refresh();
            Debug.Log(
                "IronMarch themed visual pass complete. Imported "
                + icons
                + " shop icons and wired board terrain + piece art. Open Run scene and enter Play mode.");
        }
    }
}
