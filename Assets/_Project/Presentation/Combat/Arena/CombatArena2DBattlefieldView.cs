using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Square grid battlefield using textured quads (2.5D Top Troops style).</summary>
    public static class CombatArena2DBattlefieldView
    {
        private const string RootName = "CombatArena2DGrid";
        private const float BackdropYOffset = 0.02f;
        private const float CellYOffset = 0.06f;
        private const int BackdropRenderQueue = 2440;
        private const int CellRenderQueue = 2450;

        public static Transform Build(
            Transform arenaRoot,
            BattlefieldLayout layout,
            CombatGridMapper mapper,
            CombatArenaConfigSO config,
            Camera arenaCamera = null)
        {
            if (arenaRoot == null || layout == null || mapper == null || config == null)
                return null;

            DestroyExisting(arenaRoot);

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(arenaRoot, false);
            var palette = TopTroopsBattlefieldPalette.FromConfig(config);
            float inset = 1f - Mathf.Clamp(config.gridCellInset, 0f, 0.12f);
            var art = CombatArena2DEnvironmentArt.Load();

            CreateSky(rootGo.transform, config, arenaCamera);
            CreateBackdrop(rootGo.transform, layout, config, art?.gridBackdrop);
            CreateCells(rootGo.transform, layout, mapper, config, palette, inset, art);
            CombatArena2DBattlefieldDressing.Build(rootGo.transform, layout, mapper, config);

            return rootGo.transform;
        }

        internal static bool IsLightCheckerCell(int x, int y) => (x + y) % 2 == 0;

        internal static bool IsHorizonRow(int y) => y == 0;

        /// <summary>Warm dirt tone for sky + back row (matches liked screenshot look).</summary>
        internal static Color ResolveHorizonSkyColor(CombatArenaConfigSO config) =>
            Color.Lerp(config.gridLightCellColor, config.gridDarkCellColor, 0.45f);

        private static void CreateCells(
            Transform parent,
            BattlefieldLayout layout,
            CombatGridMapper mapper,
            CombatArenaConfigSO config,
            TopTroopsBattlefieldPalette palette,
            float inset,
            CombatArena2DEnvironmentArtSO art)
        {
            bool useSprites = art != null && art.gridCellLight != null && art.gridCellDark != null;

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.TotalWidth; x++)
                {
                    var coord = new GridCoord(x, y);
                    Vector3 center = mapper.ToWorld(coord);
                    Color cellColor = palette.ResolveCellColor(layout, x, y);
                    float width = config.cellWidth * inset;
                    float depth = config.cellDepth * inset;

                    if (IsHorizonRow(y))
                    {
                        CreateColorTile(
                            parent,
                            center,
                            width,
                            depth,
                            ResolveHorizonSkyColor(config),
                            CellYOffset,
                            CellRenderQueue);
                        continue;
                    }

                    if (useSprites)
                    {
                        // One shared texture: alternating light/dark sprites read as a game
                        // board, not terrain. A whisper of checker tint keeps motion readable.
                        Color tint = CombatArena2DSpriteMaterial.ResolveZoneTint(cellColor, 0.28f);
                        if (!IsLightCheckerCell(x, y))
                            tint *= 0.955f;
                        tint.a = 1f;
                        CreateTexturedTile(parent, center, width, depth, CellYOffset, art.gridCellLight, tint, CellRenderQueue);
                    }
                    else
                        CreateColorTile(parent, center, width, depth, cellColor, CellYOffset);
                }
            }
        }

        private static void CreateBackdrop(
            Transform parent,
            BattlefieldLayout layout,
            CombatArenaConfigSO config,
            Sprite backdropSprite)
        {
            float w = layout.TotalWidth * config.cellWidth + 2f;
            float d = layout.Height * config.cellDepth + 2f;
            Vector3 center = Vector3.zero;

            if (backdropSprite != null)
            {
                CreateTiledTexturedTile(
                    parent,
                    center,
                    w,
                    d,
                    BackdropYOffset,
                    backdropSprite,
                    Color.white,
                    BackdropRenderQueue);
                return;
            }

            CreateColorTile(parent, center, w, d, config.gridBackdropColor, BackdropYOffset, BackdropRenderQueue);
        }

        private static void CreateSky(
            Transform parent,
            CombatArenaConfigSO config,
            Camera arenaCamera)
        {
            if (config == null)
                return;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "HorizonSky";
            quad.transform.SetParent(parent, false);
            ApplySolidSkyMaterial(quad, ResolveHorizonSkyColor(config));
            DestroyCollider(quad);

            if (arenaCamera != null)
                quad.AddComponent<CombatArena2DSkyBillboard>().Bind(arenaCamera);
        }

        private static void ApplySolidSkyMaterial(GameObject quad, Color color)
        {
            var renderer = quad.GetComponent<Renderer>();
            CombatArenaMaterialUtility.ApplySolidGroundMaterial(renderer, color);
            if (renderer.sharedMaterial == null)
                return;

            renderer.sharedMaterial.renderQueue = 2430;
            if (renderer.sharedMaterial.HasProperty("_ZWrite"))
                renderer.sharedMaterial.SetInt("_ZWrite", 0);
        }

        private static void CreateTiledTexturedTile(
            Transform parent,
            Vector3 center,
            float width,
            float depth,
            float yOffset,
            Sprite sprite,
            Color tint,
            int renderQueue)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Backdrop";
            quad.transform.SetParent(parent, false);
            quad.transform.position = center + Vector3.up * yOffset;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(width, depth, 1f);
            ApplyTiledSpriteMaterial(quad, sprite, tint, renderQueue, new Vector2(width, depth));
            DestroyCollider(quad);
        }

        private static void CreateTexturedTile(
            Transform parent,
            Vector3 center,
            float width,
            float depth,
            float yOffset,
            Sprite sprite,
            Color tint,
            int renderQueue)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Cell";
            quad.transform.SetParent(parent, false);
            quad.transform.position = center + Vector3.up * yOffset;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(width, depth, 1f);
            ApplySpriteMaterial(quad, sprite, tint, renderQueue);
            DestroyCollider(quad);
        }

        private static void ApplyTiledSpriteMaterial(
            GameObject quad,
            Sprite sprite,
            Color tint,
            int renderQueue,
            Vector2 worldSize)
        {
            var renderer = quad.GetComponent<Renderer>();
            var material = CombatArena2DSpriteMaterial.CreateUnlitTiled(sprite, tint, renderQueue, worldSize);
            if (material != null)
                renderer.sharedMaterial = material;
            else
                CombatArenaMaterialUtility.ApplySolidGroundMaterial(renderer, tint);
        }

        private static void ApplySpriteMaterial(GameObject quad, Sprite sprite, Color tint, int renderQueue)
        {
            var renderer = quad.GetComponent<Renderer>();
            var material = CombatArena2DSpriteMaterial.CreateUnlit(sprite, tint, renderQueue);
            if (material != null)
                renderer.sharedMaterial = material;
            else
                CombatArenaMaterialUtility.ApplySolidGroundMaterial(renderer, tint);
        }

        private static void CreateColorTile(
            Transform parent,
            Vector3 center,
            float width,
            float depth,
            Color color,
            float yOffset,
            int renderQueue = 2450)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Cell";
            quad.transform.SetParent(parent, false);
            quad.transform.position = center + Vector3.up * yOffset;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(width, depth, 1f);
            var renderer = quad.GetComponent<Renderer>();
            CombatArenaMaterialUtility.ApplySolidGroundMaterial(renderer, color);
            if (renderer.sharedMaterial != null)
                renderer.sharedMaterial.renderQueue = renderQueue;
            DestroyCollider(quad);
        }

        private static void DestroyCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }

        private static void DestroyExisting(Transform arenaRoot)
        {
            var existing = arenaRoot.Find(RootName);
            if (existing == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }
    }
}
