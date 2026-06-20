using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Removes obsolete unit-card objects left from the old hover-card system.</summary>
    public static class LegacyUnitCardCleanup
    {
        public const string FloatingLayerName = "PieceHoverCardLayer";
        public const string LegacyChildName = "UnitCard";
        public const string LegacyHoverChildName = "PieceHoverCard";

        public static void RemoveLegacyChildren(Transform host)
        {
            if (host == null)
                return;

            for (int i = host.childCount - 1; i >= 0; i--)
            {
                var child = host.GetChild(i);
                if (child == null)
                    continue;

                if (child.GetComponentInChildren<UI.PieceCardView>(true) != null)
                    continue;

                if (!IsLegacyCardObject(child))
                    continue;

                DeactivateAndDestroy(child.gameObject);
            }
        }

        public static void RemoveFloatingHoverLayers()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null)
                    continue;

                var layer = canvas.transform.Find(FloatingLayerName);
                if (layer == null)
                    continue;

                DeactivateAndDestroy(layer.gameObject);
            }
        }

        private static bool IsLegacyCardObject(Transform child)
        {
            string name = child.name;
            return name == LegacyChildName || name == LegacyHoverChildName;
        }

        private static void DeactivateAndDestroy(GameObject go)
        {
            if (go == null)
                return;

            go.SetActive(false);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(go);
                return;
            }
#endif
            Object.Destroy(go);
        }
    }
}
