#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>Builds CombatUnit2DAnimationSetSO assets from Autosprite grid sheets and
    /// assigns them to piece definitions. Sheets are square grids (512 / 256 / 128 px cells)
    /// named "{stripPrefix}_{state}.png" under Animations/{pieceId}/.</summary>
    public static class Combat2DAnimationSetBuilder
    {
        private const string AnimRoot = "Assets/_Project/Art/Combat2D/Units/Animations";
        private const string PieceRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";
        private const int SpritePixelsPerUnit = 256;
        private const int MaxAnimationTextureSize = 8192;

        private static readonly (string pieceId, string stripPrefix)[] Pieces =
        {
            ("field_medic", "field_medic"),
            ("conscript_rifleman", "conscript_rifleman"),
            ("armored_transport", "armored_transport"),
            ("ironmarch_surgeon", "ironmarch_surgeon"),
            ("bulwark_squad", "bulwark_squad"),
            ("enlisted_rifleman", "enlisted_rifleman"),
            ("ironmarch_iron_horse", "ironmarch_iron_horse"),
            ("ironclad_mortars", "ironclad_mortars"),
            ("ironclad_marksman", "ironclad_marksman"),
            ("ironclad_field_marshal", "ironclad_field_marshal"),
            ("machine_gun_nest", "machine_gun_nest"),
        };

        [MenuItem(DeadManZoneEditorMenus.CombatArena + "Build Field Medic 2D Anim Set")]
        public static void BuildFieldMedic() => BuildOne("field_medic", "medic");

        [MenuItem(DeadManZoneEditorMenus.CombatArena + "Build Bulwark Squad 2D Anim Set")]
        public static void BuildBulwarkSquad() => BuildOne("bulwark_squad", "bulwark_squad");

        [MenuItem(DeadManZoneEditorMenus.CombatArena + "Build All Unit 2D Anim Sets")]
        public static void BuildAll()
        {
            int built = 0;
            foreach (var (pieceId, prefix) in Pieces)
                built += BuildOne(pieceId, prefix) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Built {built} combat 2D anim sets.");
        }

        private static bool BuildOne(string pieceId, string stripPrefix)
        {
            string dir = $"{AnimRoot}/{pieceId}";
            string setPath = $"{dir}/{pieceId}_anim_set.asset";

            var set = AssetDatabase.LoadAssetAtPath<CombatUnit2DAnimationSetSO>(setPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
                AssetDatabase.CreateAsset(set, setPath);
            }

            // Feel targets (combatvisualv2): weighty locomotion, snappy shots, readable deaths.
            set.idle = Strip(dir, stripPrefix, "idle", targetDurationSeconds: 4f, loop: true);
            set.walk = Strip(dir, stripPrefix, "walk", targetDurationSeconds: 3.5f, loop: true);
            set.run = Strip(dir, stripPrefix, "run", targetDurationSeconds: 3f, loop: true);
            set.shoot = Strip(dir, stripPrefix, "shoot", targetDurationSeconds: 1.5f, loop: false);
            set.hurt = default;
            set.hitReact = default;
            set.die = Strip(dir, stripPrefix, "die", targetDurationSeconds: 4f, loop: false);

            if (!set.HasAny)
            {
                Debug.LogWarning($"No strips found for {pieceId} under {dir}; skipped.");
                return false;
            }

            EditorUtility.SetDirty(set);

            var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{PieceRoot}/{pieceId}.asset");
            if (piece != null)
            {
                piece.combatArena2DAnimations = set;
                EditorUtility.SetDirty(piece);
            }
            else
            {
                Debug.LogWarning($"Piece asset not found for {pieceId}");
            }

            return true;
        }

        private static CombatUnit2DStrip Strip(
            string dir,
            string prefix,
            string state,
            float targetDurationSeconds,
            bool loop)
        {
            string path = $"{dir}/{prefix}_{state}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null || sprite.texture == null)
                return default;

            EnsureAnimationImportSettings(path);

            if (!TryDetectLayout(sprite.texture, out int columns, out int frameCount))
                return default;

            return new CombatUnit2DStrip
            {
                sheet = sprite,
                frameCount = frameCount,
                columns = columns,
                framesPerSecond = CombatUnit2DStripLayout.FramesPerSecondForDuration(frameCount, targetDurationSeconds),
                loop = loop
            };
        }

        private static bool TryDetectLayout(Texture2D texture, out int columns, out int frameCount) =>
            CombatUnit2DStripLayout.TryDetectBestFromTexture(texture, out columns, out frameCount, out _);

        private static void EnsureAnimationImportSettings(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            bool changed = false;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.maxTextureSize != MaxAnimationTextureSize)
            {
                importer.maxTextureSize = MaxAnimationTextureSize;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, SpritePixelsPerUnit))
            {
                importer.spritePixelsPerUnit = SpritePixelsPerUnit;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                changed = true;
            }

            var defaultSettings = importer.GetDefaultPlatformTextureSettings();
            if (defaultSettings.maxTextureSize != MaxAnimationTextureSize)
            {
                defaultSettings.maxTextureSize = MaxAnimationTextureSize;
                importer.SetPlatformTextureSettings(defaultSettings);
                changed = true;
            }

            var standaloneSettings = importer.GetPlatformTextureSettings("Standalone");
            if (standaloneSettings.maxTextureSize != MaxAnimationTextureSize)
            {
                standaloneSettings.maxTextureSize = MaxAnimationTextureSize;
                standaloneSettings.overridden = false;
                importer.SetPlatformTextureSettings(standaloneSettings);
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }
    }
}
#endif
