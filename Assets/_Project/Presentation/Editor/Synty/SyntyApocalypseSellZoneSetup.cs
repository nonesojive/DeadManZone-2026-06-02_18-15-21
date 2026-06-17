using DeadManZone.Presentation.Visual;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Wires Interface Apocalypse HUD sell-zone art (MetalWires frame + trash-can icon).
    /// </summary>
    public static class SyntyApocalypseSellZoneSetup
    {
        public const string ApocalypseHudRoot = "Assets/Synty/InterfaceApocalypseHUD";
        public const string SellBoxSpritePath = ApocalypseHudRoot + "/Sprites/Apocalypse/SPR_Apocalypse_Box_MetalWires_01.png";
        public const string BakedTrashIconPath = "Assets/_Project/Art/UI/sell_zone_trash_can.png";

        private const string TrashCanPrefabPath =
            "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_TrashCan_01.prefab";

        private static readonly string[] TrashIconCandidatePaths =
        {
            BakedTrashIconPath,
            ApocalypseHudRoot + "/Sprites/Icons_Resources/ICON_SM_Prop_Scav_Scrap_27.png",
            ApocalypseHudRoot + "/Sprites/Icons_Resources/ICON_SM_Prop_Scav_Scrap_13.png"
        };

        private static readonly string[] ThemeAssetPaths =
        {
            "Assets/_Project/Data/Visual/Presets/SyntyTrenchUiTheme.asset",
            "Assets/_Project/Data/Visual/Presets/BunkerSurvivalUiTheme.asset",
            "Assets/_Project/Data/Resources/DeadManZone/UiTheme.asset"
        };

        [MenuItem("DeadManZone/UI Kit/Wire Apocalypse Sell Zone")]
        public static void WireApocalypseSellZone()
        {
            if (!AssetDatabase.IsValidFolder(ApocalypseHudRoot))
            {
                Debug.LogError(
                    $"Interface Apocalypse HUD not found at {ApocalypseHudRoot}. " +
                    "Import InterfaceApocalypseHUD into Assets/Synty/.");
                return;
            }

            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(SellBoxSpritePath);
            if (frame == null)
            {
                Debug.LogError($"Sell frame sprite not found: {SellBoxSpritePath}");
                return;
            }

            var icon = ResolveTrashIcon();
            if (icon == null)
            {
                Debug.LogError(
                    "No trash-can icon found. Run DeadManZone/UI Kit/Bake Sell Zone Trash Icon first.");
                return;
            }

            foreach (var path in ThemeAssetPaths)
            {
                var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(path);
                if (theme == null)
                    continue;

                theme.sellZoneSprite = frame;
                theme.sellZoneIconSprite = icon;
                EditorUtility.SetDirty(theme);
            }

            UiThemeProvider.InvalidateCache();
            UiThemeSceneRefresher.RefreshOpenScene(null);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Apocalypse sell zone wired.\n" +
                $"- Frame: {SellBoxSpritePath}\n" +
                $"- Icon: {AssetDatabase.GetAssetPath(icon)}");
        }

        [MenuItem("DeadManZone/UI Kit/Bake Sell Zone Trash Icon")]
        public static void BakeSellZoneTrashIcon()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TrashCanPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Trash can prefab not found: {TrashCanPrefabPath}");
                return;
            }

            EnsureFolder("Assets/_Project/Art/UI");

            var instance = Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.Euler(20f, -35f, 0f);
                instance.transform.localScale = Vector3.one;

                var bounds = CalculateBounds(instance);
                var center = bounds.center;
                var extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

                const int size = 256;
                var renderTexture = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
                var cameraGo = new GameObject("SellZoneIconBakeCamera", typeof(Camera));
                cameraGo.hideFlags = HideFlags.HideAndDontSave;
                try
                {
                    var camera = cameraGo.GetComponent<Camera>();
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    camera.orthographic = true;
                    camera.orthographicSize = extent * 1.15f;
                    camera.nearClipPlane = 0.01f;
                    camera.farClipPlane = extent * 8f;
                    camera.transform.position = center + new Vector3(0f, extent * 0.35f, -extent * 2.2f);
                    camera.transform.LookAt(center);
                    camera.targetTexture = renderTexture;

                    var previous = RenderTexture.active;
                    RenderTexture.active = renderTexture;
                    camera.Render();

                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    texture.Apply();
                    RenderTexture.active = previous;

                    File.WriteAllBytes(BakedTrashIconPath, texture.EncodeToPNG());
                    Object.DestroyImmediate(texture);
                }
                finally
                {
                    Object.DestroyImmediate(cameraGo);
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.ImportAsset(BakedTrashIconPath, ImportAssetOptions.ForceUpdate);
            ConfigureImportedIcon(BakedTrashIconPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Baked sell-zone trash icon to {BakedTrashIconPath}");
        }

        private static Sprite ResolveTrashIcon()
        {
            foreach (var path in TrashIconCandidatePaths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    return sprite;
            }

            return FindTrashIconBySearch();
        }

        private static Sprite FindTrashIconBySearch()
        {
            foreach (var guid in AssetDatabase.FindAssets("ICON_SM_Prop_TrashCan t:Sprite"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    return sprite;
            }

            foreach (var guid in AssetDatabase.FindAssets("ICON_SM_Prop_Rubbish t:Sprite"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    return sprite;
            }

            return null;
        }

        private static void ConfigureImportedIcon(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
