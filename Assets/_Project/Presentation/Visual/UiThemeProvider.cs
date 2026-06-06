using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    public static class UiThemeProvider
    {
        public const string ResourcePath = "DeadManZone/UiTheme";

        private static UiThemeSO _cached;

        public static UiThemeSO Current
        {
            get
            {
                if (_cached != null)
                    return _cached;

                var profileTheme = VisualProfileProvider.Current?.uiTheme;
                if (profileTheme != null)
                {
                    _cached = profileTheme;
                    return _cached;
                }

                _cached = Resources.Load<UiThemeSO>(ResourcePath);
                if (_cached == null)
                    _cached = CreateFallback();

                return _cached;
            }
        }

        public static void InvalidateCache()
        {
            _cached = null;
            VisualProfileProvider.InvalidateCache();
        }

        private static UiThemeSO CreateFallback()
        {
            var theme = ScriptableObject.CreateInstance<UiThemeSO>();
            theme.name = "UiTheme_RuntimeFallback";
            return theme;
        }
    }
}
