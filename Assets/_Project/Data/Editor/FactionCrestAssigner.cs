#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Wave 4 faction-select overhaul: assigns each FactionSO's <see cref="FactionSO.crest"/>
    /// sprite from the icon-forge crest set (Assets/_Project/Data/Resources/DeadManZone/Icons/
    /// Crests/icon_&lt;factionId&gt;.png), and makes sure those textures import as Sprites (new
    /// PNGs default to the wrong texture type — see dmz-icon-forge SKILL.md's import-settings
    /// step). Idempotent: safe to re-run any time, including from MenuSceneSetup.SetupScenes.
    /// FactionSelectView also has a Resources.Load fallback for the same path, so the faction
    /// select screen isn't sprite-less if this pass hasn't run yet.
    /// </summary>
    public static class FactionCrestAssigner
    {
        private const string CrestsFolder = "Assets/_Project/Data/Resources/DeadManZone/Icons/Crests";

        [MenuItem("DeadManZone/Content/Assign Faction Crests")]
        public static void Assign()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Debug.LogWarning("[FactionCrestAssigner] ContentDatabase not found — run " +
                                 "'DeadManZone/Generate Demo Content' first.");
                return;
            }

            int assigned = 0, missing = 0;
            foreach (var factionId in ContentDatabase.PlayableFactionIds)
            {
                var faction = database.GetFaction(factionId);
                if (faction == null)
                    continue;

                string pngPath = $"{CrestsFolder}/icon_{factionId}.png";
                ApplySpriteImportSettings(pngPath);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                if (sprite == null)
                {
                    missing++;
                    Debug.LogWarning($"[FactionCrestAssigner] No crest sprite at {pngPath} for '{factionId}'.");
                    continue;
                }

                if (faction.crest == sprite)
                    continue;

                faction.crest = sprite;
                EditorUtility.SetDirty(faction);
                assigned++;
            }

            if (assigned > 0)
                AssetDatabase.SaveAssets();

            Debug.Log($"[FactionCrestAssigner] Assigned {assigned} crest(s), {missing} missing.");
        }

        private static void ApplySpriteImportSettings(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }
    }
}
#endif
