using DeadManZone.Core.Combat;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Small world-space HP bar above a combat unit. Hidden at full health
    /// (Top Troops style) so intact lines stay clean; appears on first damage.</summary>
    public sealed class CombatUnitHealthBar : MonoBehaviour
    {
        private const float Width = 0.62f;
        private const float Height = 0.085f;
        private const float HeadClearance = 0.22f; // gap above the head
        // Units are camera-facing billboards under an angled arena camera, so the rendered
        // head projects higher on screen than a plain world-vertical offset predicts. Lift
        // the bar proportionally so it clears the head instead of sitting at shoulder height.
        private const float HeadHeightFactor = 1.5f;

        private static readonly Color AllyFill = new(0.38f, 0.84f, 0.34f, 1f);
        private static readonly Color EnemyFill = new(0.92f, 0.32f, 0.25f, 1f);
        private static readonly Color Background = new(0.07f, 0.055f, 0.045f, 0.85f);

        private Transform _fill;
        private GameObject _root;
        private float _fraction = 1f;

        public static CombatUnitHealthBar Attach(
            CombatUnitActor actor,
            CombatSide side,
            Camera arenaCamera,
            float headHeight)
        {
            if (actor == null)
                return null;

            var bar = actor.gameObject.GetComponent<CombatUnitHealthBar>();
            if (bar == null)
                bar = actor.gameObject.AddComponent<CombatUnitHealthBar>();

            bar.BuildVisual(side, arenaCamera, headHeight * HeadHeightFactor + HeadClearance);
            return bar;
        }

        public void SetFraction(float fraction)
        {
            _fraction = Mathf.Clamp01(fraction);

            if (_root == null)
                return;

            // Hidden while untouched; dead units hide too (death anim carries the story).
            bool visible = _fraction < 0.999f && _fraction > 0f;
            _root.SetActive(visible);

            if (_fill != null)
            {
                var scale = _fill.localScale;
                scale.x = Width * _fraction;
                _fill.localScale = scale;
                var local = _fill.localPosition;
                local.x = -Width * 0.5f * (1f - _fraction);
                _fill.localPosition = local;
            }
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        public void Clear()
        {
            if (_root != null)
                Destroy(_root);
            _root = null;
            _fill = null;
            _fraction = 1f;
        }

        private void BuildVisual(CombatSide side, Camera arenaCamera, float headHeight)
        {
            Clear();

            _root = new GameObject("HealthBar");
            _root.transform.SetParent(transform, false);
            _root.transform.localPosition = new Vector3(0f, headHeight, 0f);
            if (arenaCamera != null)
                _root.AddComponent<CombatArena2DSpriteBillboard>().Bind(arenaCamera);

            // Mesh quads + arena sprite materials: SpriteRenderers do not draw with
            // this project's URP renderer setup (unit sprites hit the same issue).
            int renderQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(transform.position.z, 70);
            CreateQuad("Bg", _root.transform,
                new Vector3(Width + 0.05f, Height + 0.05f, 1f),
                new Vector3(0f, 0f, 0.001f), Background, renderQueue);
            var fillGo = CreateQuad("Fill", _root.transform,
                new Vector3(Width, Height, 1f),
                Vector3.zero, side == CombatSide.Enemy ? EnemyFill : AllyFill, renderQueue + 1);
            _fill = fillGo.transform;

            _root.SetActive(false);
        }

        private static GameObject CreateQuad(
            string name,
            Transform parent,
            Vector3 scale,
            Vector3 localPosition,
            Color color,
            int renderQueue)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            var material = CombatArena2DSpriteMaterial.CreateSprite(
                CombatArena2DPlaceholderSprites.WhitePixel,
                color,
                renderQueue,
                softAlpha: true);
            if (material != null)
                renderer.sharedMaterial = material;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            return go;
        }
    }
}
