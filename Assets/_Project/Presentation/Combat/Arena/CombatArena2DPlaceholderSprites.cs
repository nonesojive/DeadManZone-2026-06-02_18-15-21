using DeadManZone.Core.Combat;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Runtime-generated placeholder sprites for 2D combat (ponytail: replace with atlas when art lands).</summary>
    public static class CombatArena2DPlaceholderSprites
    {
        private static Sprite _defaultSilhouette;
        private static readonly System.Collections.Generic.Dictionary<CombatArena2DSilhouetteRole, Sprite> RoleSprites = new();

        public static Sprite DefaultSilhouette =>
            CombatArena2DSilhouetteArt.ForRole(CombatArena2DSilhouetteRole.Generic)
            ?? (_defaultSilhouette ??= CreateSilhouette(0.45f, 0.55f, 0.35f));

        public static Sprite ForRole(PieceDefinitionSO piece, CombatSide side, Color categoryTint)
        {
            var mapped = CombatUnitSpriteResolver.MapRole(piece);
            var artSprite = CombatArena2DSilhouetteArt.ForRole(mapped);
            if (artSprite != null)
                return artSprite;

            if (!RoleSprites.TryGetValue(mapped, out var sprite) || sprite == null)
            {
                sprite = CreateRoleShape(mapped);
                RoleSprites[mapped] = sprite;
            }

            return sprite;
        }

        public static Sprite Shadow => CreateSilhouette(0f, 0f, 0f, alpha: 0.35f, size: 32);

        // ponytail: CreateSilhouette(size:4) yields a 4x1 all-clear texture (h clamps to 1,
        // ellipse test divides by zero) — WhitePixel never rendered. Kept for callers that
        // want the legacy name; SolidWhite below is the actually-opaque quad fill.
        public static Sprite WhitePixel => SolidWhite;

        private static Sprite _solidWhite;

        /// <summary>Fully opaque 2x2 white sprite for tinted UI/VFX quads.</summary>
        public static Sprite SolidWhite
        {
            get
            {
                if (_solidWhite == null)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point
                    };
                    tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    tex.Apply();
                    _solidWhite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
                    _solidWhite.name = "SolidWhite";
                }

                return _solidWhite;
            }
        }

        private static Sprite CreateRoleShape(CombatArena2DSilhouetteRole role)
        {
            return role switch
            {
                CombatArena2DSilhouetteRole.Artillery => CreateSilhouette(0.35f, 0.32f, 0.28f, wide: true),
                CombatArena2DSilhouetteRole.Ranged => CreateSilhouette(0.38f, 0.42f, 0.32f, tall: true),
                CombatArena2DSilhouetteRole.Assault => CreateSilhouette(0.42f, 0.36f, 0.30f),
                CombatArena2DSilhouetteRole.Vehicle => CreateSilhouette(0.32f, 0.34f, 0.38f, wide: true),
                _ => CreateSilhouette(0.40f, 0.38f, 0.34f)
            };
        }

        private static Sprite CreateSilhouette(
            float r,
            float g,
            float b,
            float alpha = 1f,
            int size = 64,
            bool wide = false,
            bool tall = false)
        {
            int w = Mathf.Max(1, wide ? size + 16 : size);
            int h = Mathf.Max(1, tall ? size + 12 : size - 8);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var fill = new Color(r, g, b, alpha);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f;
                    float ny = (y / (float)(h - 1)) * 2f - 1f;
                    bool inside = nx * nx + ny * ny * 1.2f <= 0.85f;
                    tex.SetPixel(x, y, inside ? fill : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 64f);
        }
    }
}
