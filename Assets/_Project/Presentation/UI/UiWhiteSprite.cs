using UnityEngine;

namespace DeadManZone.Presentation.UI
{
    /// <summary>Shared 1×1 white sprite for UI fills (Filled Image type requires a sprite).</summary>
    public static class UiWhiteSprite
    {
        private static Sprite s_Instance;

        public static Sprite Get()
        {
            if (s_Instance != null)
                return s_Instance;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            s_Instance = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            return s_Instance;
        }
    }
}
