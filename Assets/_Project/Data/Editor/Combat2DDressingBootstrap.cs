using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>Wires the WW1 trench tileset sheets into the battlefield dressing SO.
    /// Sets the source textures readable so the prop slicer can alpha-crop them.</summary>
    public static class Combat2DDressingBootstrap
    {
        private const string SandbagSheetPath = "Assets/_Project/Art/Tilesets/WW1 trench/1 (1).png";
        private const string WireSheetPath = "Assets/_Project/Art/Tilesets/WW1 trench/3 (1).png";
        private const string RuinsSheetPath = "Assets/_Project/Art/Tilesets/WW1 ruins/4.png";
        private const string OutputPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArena2DDressingArt.asset";

        [MenuItem(DeadManZoneEditorMenus.CombatArena + "Setup Battlefield Dressing Art")]
        public static void Setup()
        {
            var sandbags = EnsureReadable(SandbagSheetPath);
            var wire = EnsureReadable(WireSheetPath);
            var ruins = EnsureReadable(RuinsSheetPath);
            if (sandbags == null || wire == null || ruins == null)
            {
                Debug.LogError("[Dressing] WW1 tileset sheets not found; dressing art not created.");
                return;
            }

            var art = AssetDatabase.LoadAssetAtPath<CombatArena2DDressingArtSO>(OutputPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<CombatArena2DDressingArtSO>();
                AssetDatabase.CreateAsset(art, OutputPath);
            }

            art.sandbagSheet = sandbags;
            art.wireSheet = wire;
            art.ruinsSheet = ruins;
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();

            // Combat VFX strips need readability so the runtime radial mask can bake;
            // unmasked, their square cell haze flashes as a box mid-field.
            var vfx = AssetDatabase.LoadAssetAtPath<CombatArena2DVfxArtSO>(
                "Assets/_Project/Data/Resources/DeadManZone/CombatArena2DVfxArt.asset");
            if (vfx != null)
            {
                EnsureSpriteTextureReadable(vfx.rifleImpactStrip);
                EnsureSpriteTextureReadable(vfx.explosionSmallStrip);
                EnsureSpriteTextureReadable(vfx.deathPuffStrip);
            }

            Debug.Log($"[Dressing] Battlefield dressing art ready at {OutputPath}.");
        }

        private static void EnsureSpriteTextureReadable(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return;

            string path = AssetDatabase.GetAssetPath(sprite.texture);
            if (!string.IsNullOrEmpty(path))
                EnsureReadable(path);
        }

        private static Texture2D EnsureReadable(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;

            if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
