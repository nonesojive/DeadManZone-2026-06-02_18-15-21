using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Procedural Top Troops-style battlefield: zone-tinted flat cells, ground plane, cliffs, and props.
    /// Ported from TopTroopsCombat BattlefieldVisual and aligned to DeadManZone CombatGridMapper.
    /// </summary>
    public static class TopTroopsBattlefieldBuilder
    {
        private const string RootName = "TopTroopsBattlefield";
        private const float CellHeight = 0.04f;
        private const float CellLift = 0.02f;
        private const float CellInset = 0.95f;

        public static TopTroopsBattlefieldView Build(
            Transform arenaRoot,
            BattlefieldLayout layout,
            float cellWidth,
            float cellDepth,
            TopTroopsBattlefieldPalette palette)
        {
            if (arenaRoot == null || layout == null)
                return null;

            DestroyExisting(arenaRoot);

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(arenaRoot, false);
            var view = rootGo.AddComponent<TopTroopsBattlefieldView>();

            var gridRoot = new GameObject("GridRoot").transform;
            gridRoot.SetParent(rootGo.transform, false);
            view.SetGridRoot(gridRoot);

            var envRoot = new GameObject("EnvironmentRoot").transform;
            envRoot.SetParent(rootGo.transform, false);

            var mapper = new CombatGridMapper(layout, cellWidth, cellDepth);
            BuildCells(gridRoot, layout, mapper, cellWidth, cellDepth, palette);
            BuildEnvironment(envRoot, layout, cellWidth, cellDepth);
            BuildNeutralDivider(rootGo.transform, layout, mapper, cellWidth, cellDepth);

            return view;
        }

        public static Color ResolveCellColor(BattlefieldLayout layout, int x, int y, TopTroopsBattlefieldPalette palette)
        {
            Color zone;
            if (layout.IsPlayerHalf(x))
                zone = palette.PlayerZoneColor;
            else if (layout.IsNeutralColumn(x))
                zone = palette.NeutralZoneColor;
            else
                zone = palette.EnemyZoneColor;

            float checkerShade = GetCheckerShade(x, y);
            return ApplyCheckerShade(zone, checkerShade);
        }

        public static float GetCheckerShade(int x, int y) => (x + y) % 2 == 0 ? 1f : 0.86f;

        public static Color ApplyCheckerShade(Color zone, int x, int y) =>
            ApplyCheckerShade(zone, GetCheckerShade(x, y));

        public static Color ApplyCheckerShade(Color zone, float shade) => zone * shade;

        private static void BuildCells(
            Transform gridRoot,
            BattlefieldLayout layout,
            CombatGridMapper mapper,
            float cellWidth,
            float cellDepth,
            TopTroopsBattlefieldPalette palette)
        {
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.TotalWidth; x++)
                {
                    var coord = new GridCoord(x, y);
                    Vector3 center = mapper.ToWorld(coord);
                    var cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cell.name = $"Cell_{x}_{y}";
                    cell.transform.SetParent(gridRoot, false);
                    cell.transform.position = center + Vector3.up * CellLift;
                    cell.transform.localScale = new Vector3(cellWidth * CellInset, CellHeight, cellDepth * CellInset);

                    var renderer = cell.GetComponent<Renderer>();
                    renderer.sharedMaterial = TopTroopsMaterialLibrary.CreateCellMaterial(
                        ResolveCellColor(layout, x, y, palette));

                    DestroyCollider(cell);
                }
            }
        }

        private static void BuildEnvironment(
            Transform envRoot,
            BattlefieldLayout layout,
            float cellWidth,
            float cellDepth)
        {
            float width = layout.TotalWidth * cellWidth;
            float depth = layout.Height * cellDepth;
            var center = new Vector3(0f, 0f, 0f);

            CreateGround(envRoot, center, width + 8f, depth + 8f);
            CreateCliff(envRoot, center + new Vector3(-width * 0.5f - 3f, 1f, 0f), new Vector3(4f, 2f, depth + 10f));
            CreateCliff(envRoot, center + new Vector3(width * 0.5f + 3f, 1f, 0f), new Vector3(4f, 2f, depth + 10f));
            CreateProp(envRoot, center + new Vector3(-width * 0.5f - 1f, 0.4f, -depth * 0.3f), new Vector3(1.2f, 0.8f, 0.6f));
            CreateProp(envRoot, center + new Vector3(width * 0.5f + 1f, 0.4f, depth * 0.25f), new Vector3(1f, 0.8f, 1f));
        }

        private static void BuildNeutralDivider(
            Transform root,
            BattlefieldLayout layout,
            CombatGridMapper mapper,
            float cellWidth,
            float cellDepth)
        {
            float dividerX = (layout.NeutralStartX + layout.NeutralWidth * 0.5f - layout.TotalWidth * 0.5f) * cellWidth;
            float zTop = mapper.ToWorld(new GridCoord(0, 0)).z + cellDepth * 0.5f;
            float zBottom = mapper.ToWorld(new GridCoord(0, layout.Height - 1)).z - cellDepth * 0.5f;

            var lineGo = new GameObject("NeutralDivider");
            lineGo.transform.SetParent(root, false);
            var line = lineGo.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.95f, 0.88f, 0.65f, 0.9f);
            line.endColor = new Color(0.95f, 0.88f, 0.65f, 0.9f);
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(dividerX, 0.15f, zTop));
            line.SetPosition(1, new Vector3(dividerX, 0.15f, zBottom));
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.useWorldSpace = true;
        }

        private static void CreateGround(Transform parent, Vector3 center, float width, float depth)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "BattlefieldGround";
            ground.transform.SetParent(parent, false);
            ground.transform.position = center + Vector3.down * 0.2f;
            ground.transform.localScale = new Vector3(width, 0.2f, depth);
            ground.GetComponent<Renderer>().sharedMaterial = TopTroopsMaterialLibrary.CreateGroundMaterial();
            DestroyCollider(ground);
        }

        private static void CreateCliff(Transform parent, Vector3 position, Vector3 scale)
        {
            var cliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliff.name = "Cliff";
            cliff.transform.SetParent(parent, false);
            cliff.transform.position = position;
            cliff.transform.localScale = scale;
            cliff.GetComponent<Renderer>().sharedMaterial = TopTroopsMaterialLibrary.CreateCliffMaterial();
            DestroyCollider(cliff);
        }

        private static void CreateProp(Transform parent, Vector3 position, Vector3 scale)
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = "Sandbag";
            prop.transform.SetParent(parent, false);
            prop.transform.position = position;
            prop.transform.localScale = scale;
            prop.GetComponent<Renderer>().sharedMaterial = TopTroopsMaterialLibrary.CreatePropMaterial();
            DestroyCollider(prop);
        }

        private static void DestroyCollider(GameObject obj)
        {
            if (obj == null)
                return;

            var collider = obj.GetComponent<Collider>();
            if (collider == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }

        private static void DestroyCollider(Component component)
        {
            if (component == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(component);
            else
                Object.DestroyImmediate(component);
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
