using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    public static class VisualProfileProvider
    {
        public const string ResourcePath = "DeadManZone/VisualProfile";
        private static VisualProfileSO _cached;

        public static VisualProfileSO Current
        {
            get
            {
                if (_cached != null)
                    return _cached;
                _cached = Resources.Load<VisualProfileSO>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;
    }
}
