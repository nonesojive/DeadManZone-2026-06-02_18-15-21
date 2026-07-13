using UnityEngine;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>
    /// Single source of truth for "is ShopV2Canvas the live shop surface?".
    ///
    /// Legacy Run/shop chrome (FrontReportPanel, CriticalMassDrawerBootstrap, ...) is built at
    /// runtime on its own top-level overlay canvas at a HIGHER sorting order than ShopV2Canvas
    /// (250/300 vs. 10), so it paints over V2 and duplicates its controls. Legacy builders ask
    /// here before spawning.
    /// </summary>
    public static class ShopV2Surface
    {
        public const string CanvasName = "ShopV2Canvas";

        private static GameObject _canvas;

        /// <summary>
        /// True when the ShopV2 canvas EXISTS — i.e. V2 owns the shop, whether or not it is
        /// currently being drawn. Deliberately NOT "is it visible": hiding the shop for combat
        /// must not make the legacy FrontReportPanel start spawning again mid-fight.
        /// </summary>
        public static bool IsActive
        {
            get
            {
                // GameObject.Find only returns ACTIVE objects. Visibility is toggled via the
                // Canvas COMPONENT (see SetVisible), never the GameObject, so this stays true
                // across a hide and the cache never goes stale on us.
                if (_canvas == null)
                    _canvas = GameObject.Find(CanvasName);

                return _canvas != null && _canvas.activeInHierarchy;
            }
        }

        /// <summary>The ShopV2 canvas, or null when V2 is not the surface.</summary>
        public static Canvas Canvas => IsActive ? _canvas.GetComponent<Canvas>() : null;

        /// <summary>
        /// True only while the V2 shop is actually ON SCREEN. Distinct from <see cref="IsActive"/>:
        /// during combat V2 still OWNS the shop (so legacy shop chrome must stay retired) but is
        /// not drawn (so combat-time HUD like the run meta strip is free to come back).
        /// </summary>
        public static bool IsVisible
        {
            get
            {
                var canvas = Canvas;
                return canvas != null && canvas.enabled;
            }
        }

        /// <summary>
        /// Show/hide the V2 shop. Toggles the Canvas COMPONENT rather than the GameObject:
        /// disabling the Canvas stops both rendering and raycasting, but leaves the object
        /// active so GameObject.Find keeps resolving it and presenters stay subscribed to
        /// RunStateChanged (they need to be current the moment the shop comes back).
        /// </summary>
        public static void SetVisible(bool visible)
        {
            var canvas = Canvas;
            if (canvas != null)
                canvas.enabled = visible;
        }

        /// <summary>Test seam: drops the cached lookup.</summary>
        public static void ResetCache() => _canvas = null;
    }
}
