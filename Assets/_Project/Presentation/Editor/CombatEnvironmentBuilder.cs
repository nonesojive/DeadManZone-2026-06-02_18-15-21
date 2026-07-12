#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Builds a Combat3D battlefield environment for one Arena Theme (M4): broken-earth
    /// ground mesh with shell craters outside the combat strip, plus the theme's prop sets
    /// (ArenaThemeProfile toggles shared machinery — trenchworks, wire, tank traps, poles,
    /// ruin backdrop — and per-theme extras dispatch on ThemeId). Called by the scene
    /// bootstraps; fully deterministic (fixed seed + hash noise) so every theme's scene
    /// regenerates identically.
    ///
    /// Grounding contract (ALL themes): CombatArenaVisualPlacement.PlaceOnGround puts unit
    /// feet at world y = 0, so the combat strip (17x6 cells at 1.8 m = +/-15.3 x +/-5.4,
    /// plus margin) is kept EXACTLY flat — displacement only ramps in outside it (asserted
    /// every build). Environment is static-batching friendly: shared primitive meshes + a
    /// handful of materials, no colliders, no runtime scripts.
    /// </summary>
    public static class CombatEnvironmentBuilder
    {
        private const int Seed = 20260711;

        /// <summary>Active theme for the current Build() — editor-time single-threaded;
        /// set first thing in Build and read by GroundHeight/paths (threading it through
        /// every helper signature bought nothing but noise).</summary>
        private static ArenaThemeProfile _p;

        private static string GroundMeshPath => _p.EnvFolder + "/Combat3D_GroundMesh.asset";
        private static string GroundAlbedoPath => _p.EnvFolder + "/Combat3D_GroundAlbedo.asset";
        private static string DetailNoisePath => _p.EnvFolder + "/Combat3D_GroundDetail.asset";
        private static string RidgeMeshPath => _p.EnvFolder + "/Combat3D_RidgeMesh.asset";

        // Combat strip: 17x6 cells at 1.8 m, centered at origin, + safety margin.
        private const float StripHalfX = 16.4f;
        private const float StripHalfZ = 6.4f;
        private const float BlendStart = 0.5f;  // meters outside strip before displacement
        private const float BlendEnd = 5.5f;    // fully displaced beyond this

        // Ground rectangle (asymmetric: more field on +z, the far/visible side).
        private const float GroundMinX = -70f, GroundMaxX = 70f;
        private const float GroundMinZ = -40f, GroundMaxZ = 60f;

        /// <summary>Builds one theme's Environment root in the active scene and returns it.
        /// Null profile = Trenchline (pre-M4 callers: the Combat3D demo scene).</summary>
        public static GameObject Build(ArenaThemeProfile profile = null)
        {
            _p = profile ?? CombatArenaThemeProfiles.Trenchline;
            _ridgeMesh = null; // per-theme asset path — never reuse across builds
            EnsureFolder(_p.EnvFolder);
            // One sequential rng per build: within a theme the step list is fixed, so
            // regeneration is exact. Different themes consume differently — that's fine,
            // determinism is per-theme.
            var rng = new System.Random(Seed);

            var groundMat = EnsureGroundMaterial();
            var sandbag = EnsureToonInkMaterial(_p.MaterialPath("Sandbag"), new Color(0.42f, 0.37f, 0.27f), outline: 2f);
            // Kept dark: under the 1.7x warm key anything brighter reads as the most
            // saturated thing on screen, and that budget belongs to VFX (bible §3).
            var timber = EnsureToonInkMaterial(_p.MaterialPath("Timber"), new Color(0.16f, 0.12f, 0.08f), outline: 2f);
            var crate = EnsureToonInkMaterial(_p.MaterialPath("Crate"), new Color(0.35f, 0.28f, 0.18f), outline: 2f);
            var wire = EnsureToonInkMaterial(_p.MaterialPath("Wire"), new Color(0.10f, 0.10f, 0.11f), outline: 0f);
            var steel = EnsureToonInkMaterial(_p.MaterialPath("Steel"), new Color(0.17f, 0.18f, 0.20f), outline: 2f);
            var barrel = EnsureToonInkMaterial(_p.MaterialPath("Barrel"), new Color(0.30f, 0.17f, 0.10f), outline: 2f);
            // Backdrop must be URP/Lit: DMZ/ToonInk does not apply scene fog, and the whole
            // point of the ring is fog-faded silhouettes (ToonInk rendered it pitch black).
            // Value sits above the ground mud so ruins read as hazed masses, not black cutouts.
            var backdrop = EnsureLitMaterial(_p.MaterialPath("Backdrop"), new Color(0.135f, 0.128f, 0.118f));

            var root = new GameObject("Environment");
            BuildGround(root.transform, groundMat);
            if (_p.Trenchworks)
            {
                BuildTrenchworks(root.transform, sandbag, timber, crate, barrel, -1f, rng);
                BuildTrenchworks(root.transform, sandbag, timber, crate, barrel, +1f, rng);
            }

            if (_p.WireLines)
                BuildWireLines(root.transform, timber, wire, rng);
            if (_p.TankTraps)
                BuildTankTraps(root.transform, steel, rng);
            BuildMidgroundScatter(root.transform, timber, rng);
            if (_p.RuinBackdrop)
                BuildBackdrop(root.transform, backdrop, rng);

            // Per-theme dressing on top of the shared sets.
            switch (_p.ThemeId)
            {
                case Core.Run.ArenaThemes.FogField:
                    BuildFogFieldRemnants(root.transform, timber, steel, rng);
                    break;
                case Core.Run.ArenaThemes.RavagedTown:
                    // Theme-local dimmed bags: the shared sandbag color sits mid-frame
                    // here (not at Trenchline's cropped top edge) and read as the most
                    // saturated thing on screen — that budget belongs to VFX (bible §3).
                    // 0.30 still read pale under the 1.7x key — bags must sit BELOW the
                    // ground's dry tone, or they're the frame's brightest mass.
                    var dustBag = EnsureToonInkMaterial(
                        _p.MaterialPath("SandbagDust"), new Color(0.205f, 0.185f, 0.150f), outline: 2f);
                    BuildTownRuins(root.transform, backdrop, timber, dustBag, rng);
                    break;
                case Core.Run.ArenaThemes.WartornForest:
                    BuildForestDeadfall(root.transform, timber, backdrop, rng);
                    break;
            }

            MarkStatic(root);

            AssertLanesFlat();
            return root;
        }

        // ------------------------------------------------------------------ terrain

        /// <summary>0 inside the flat combat strip, ramps to 1 in the broken-earth field.</summary>
        private static float EdgeMask(float x, float z)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(x) - StripHalfX);
            float dz = Mathf.Max(0f, Mathf.Abs(z) - StripHalfZ);
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BlendStart, BlendEnd, d));
        }

        /// <summary>Terrain height. Shared by the ground mesh AND every prop placement.</summary>
        public static float GroundHeight(float x, float z)
        {
            float m = EdgeMask(x, z);
            if (m <= 0f)
                return 0f;

            float n = ValueNoise(x * 0.10f, z * 0.10f) * 0.7f
                    + ValueNoise(x * 0.33f + 17.3f, z * 0.33f + 9.1f) * 0.3f;
            float h = (n - 0.5f) * 1.1f;

            // Rise toward the fog line so the visible band tilts up into a churned-earth
            // horizon (camera is only 8.5 m up — the frame top is ground at ~z 36, so the
            // horizon treatment has to happen inside that band, not at 60 m).
            float dist = Mathf.Sqrt(x * x + z * z);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 42f, dist));
            h += rise * 3.0f;
            // Jagged skyline: extra noise that only exists on the risen crest, so the
            // fog-line silhouette reads broken/churned instead of a smooth lump.
            h += rise * (ValueNoise(x * 0.18f + 53f, z * 0.18f + 27f) - 0.5f) * 2.6f;

            foreach (var c in _p.Craters)
            {
                float r = Vector2.Distance(new Vector2(x, z), new Vector2(c.x, c.y)) / c.z;
                if (r < 1f)
                {
                    float bowl = (1f - r * r);
                    h -= c.w * bowl * bowl;
                }
                else if (r < 1.45f)
                {
                    float rim = 1f - Mathf.Abs(r - 1.2f) / 0.25f;
                    if (rim > 0f)
                        h += c.w * 0.28f * rim;
                }
            }

            return h * m;
        }

        private static void BuildGround(Transform parent, Material groundMat)
        {
            const int nx = 100, nz = 72;
            var mesh = new Mesh { name = "Combat3D_GroundMesh" };
            var verts = new Vector3[(nx + 1) * (nz + 1)];
            var uvs = new Vector2[verts.Length];
            for (int j = 0; j <= nz; j++)
            {
                float z = Mathf.Lerp(GroundMinZ, GroundMaxZ, (float)j / nz);
                for (int i = 0; i <= nx; i++)
                {
                    float x = Mathf.Lerp(GroundMinX, GroundMaxX, (float)i / nx);
                    int v = j * (nx + 1) + i;
                    verts[v] = new Vector3(x, GroundHeight(x, z), z);
                    uvs[v] = new Vector2(
                        Mathf.InverseLerp(GroundMinX, GroundMaxX, x),
                        Mathf.InverseLerp(GroundMinZ, GroundMaxZ, z));
                }
            }

            var tris = new int[nx * nz * 6];
            int t = 0;
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                int v = j * (nx + 1) + i;
                tris[t++] = v; tris[t++] = v + nx + 1; tris[t++] = v + 1;
                tris[t++] = v + 1; tris[t++] = v + nx + 1; tris[t++] = v + nx + 2;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            SaveAsset(mesh, GroundMeshPath);

            var go = new GameObject("Battlefield_Ground");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = groundMat;
        }

        private static Material EnsureGroundMaterial()
        {
            var albedo = BakeGroundAlbedo();
            var detail = BakeDetailNoise();
            string groundPath = _p.MaterialPath("Ground");
            var material = AssetDatabase.LoadAssetAtPath<Material>(groundPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, groundPath);
            }

            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", albedo);
            material.SetFloat("_Smoothness", 0.02f);
            // Tiled grey-noise detail hides the world-map albedo's magnification blur
            // where the camera is close (~3.3 m repeat over the 140x100 m ground).
            material.SetTexture("_DetailAlbedoMap", detail);
            material.SetTextureScale("_DetailAlbedoMap", new Vector2(70f, 50f));
            material.SetFloat("_DetailAlbedoMapScale", 0.8f);
            material.EnableKeyword("_DETAIL_MULX2");
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Small tileable grey noise (wrapped hash lattice) used as a detail map.</summary>
        private static Texture2D BakeDetailNoise()
        {
            const int size = 256;
            const float cells = 16f; // lattice cells across the texture — wraps seamlessly
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Combat3D_GroundDetail",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };

            var pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float u = px * cells / size, v = py * cells / size;
                float n = PeriodicNoise(u, v, (int)cells) * 0.6f + PeriodicNoise(u * 4f, v * 4f, (int)cells * 4) * 0.4f;
                float g = 0.5f + (n - 0.5f) * 0.35f; // detail maps multiply x2 around mid-grey
                pixels[py * size + px] = new Color(g, g, g, 1f);
            }

            tex.SetPixels(pixels);
            tex.Apply(true);
            SaveAsset(tex, DetailNoisePath);
            return tex;
        }

        private static float PeriodicNoise(float x, float y, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            int xa = ((x0 % period) + period) % period, xb = (xa + 1) % period;
            int ya = ((y0 % period) + period) % period, yb = (ya + 1) % period;
            float a = Hash01(xa, ya), b = Hash01(xb, ya), c = Hash01(xa, yb), d = Hash01(xb, yb);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        /// <summary>Bakes mud/dry-earth tonal variation, crater scorch, and faint marching-lane
        /// ruts (spec §2: grid fades to diegetic ground markings) into one world-mapped texture.</summary>
        private static Texture2D BakeGroundAlbedo()
        {
            const int size = 2048; // 1024 was ~7 px/m — visibly mushy at the near camera
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Combat3D_GroundAlbedo",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4, // ground is always viewed obliquely
            };

            var mud = _p.Mud;
            var dry = _p.Dry;
            var scorch = _p.Scorch;
            var pixels = new Color[size * size];

            for (int py = 0; py < size; py++)
            {
                float z = Mathf.Lerp(GroundMinZ, GroundMaxZ, (py + 0.5f) / size);
                for (int px = 0; px < size; px++)
                {
                    float x = Mathf.Lerp(GroundMinX, GroundMaxX, (px + 0.5f) / size);

                    float patchTone = ValueNoise(x * 0.07f + 11f, z * 0.07f + 29f); // broad mud fields
                    float tone = ValueNoise(x * 0.15f + 31f, z * 0.15f + 5f);
                    float fine = ValueNoise(x * 0.9f + 71f, z * 0.9f + 43f);
                    var c = Color.Lerp(mud, dry, Mathf.Clamp01(
                        0.15f + patchTone * 0.5f + tone * 0.35f + (fine - 0.5f) * 0.28f));

                    // Crater interiors darken toward scorched centers.
                    foreach (var cr in _p.Craters)
                    {
                        float r = Vector2.Distance(new Vector2(x, z), new Vector2(cr.x, cr.y)) / (cr.z * 1.3f);
                        if (r < 1f)
                            c = Color.Lerp(c, scorch, Mathf.Pow(1f - r, 1.4f) * 0.85f);
                    }

                    // Broken wheel/boot ruts along the six marching lanes.
                    if (Mathf.Abs(x) < 15.6f && Mathf.Abs(z) < 5.6f)
                    {
                        float lane = Mathf.Repeat(z + 5.4f, 1.8f) - 0.9f; // distance to lane center
                        float dz = Mathf.Abs(lane);
                        if (dz < 0.22f)
                        {
                            float laneIndex = Mathf.Floor((z + 5.4f) / 1.8f);
                            float patch = Mathf.SmoothStep(0.42f, 0.8f,
                                ValueNoise(x * 0.55f + laneIndex * 7.3f, laneIndex * 3.7f));
                            float rut = (1f - dz / 0.22f) * patch;
                            c = Color.Lerp(c, c * 0.68f, rut);
                        }
                    }

                    // Sparse dark pockmarks/shell-scatter speckle (albedo only — geometry
                    // stays flat inside the strip; sells "churned battlefield" up close).
                    float speck = ValueNoise(x * 2.3f + 101f, z * 2.3f + 57f);
                    if (speck > 0.78f)
                        c *= Mathf.Lerp(1f, 0.62f, (speck - 0.78f) / 0.22f);

                    pixels[py * size + px] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true);
            SaveAsset(tex, GroundAlbedoPath);
            return tex;
        }

        // ------------------------------------------------------------------ trenchworks

        /// <summary>Sandbag parapet + timber posts + duckboards + crate dumps at one
        /// deployment edge (side = -1 player/west, +1 enemy/east).</summary>
        private static void BuildTrenchworks(
            Transform parent, Material sandbag, Material timber, Material crate, Material barrel,
            float side, System.Random rng)
        {
            var group = new GameObject(side < 0f ? "Trenchworks_West" : "Trenchworks_East");
            group.transform.SetParent(parent, false);
            float x0 = side * 16.7f;

            // Two stacked, staggered sandbag rows with a sally-port gap mid-line.
            for (int row = 0; row < 2; row++)
            {
                float y = row == 0 ? 0.15f : 0.44f;
                float zStart = -6.2f + (row == 1 ? 0.31f : 0f);
                for (float z = zStart; z <= 6.2f; z += 0.63f)
                {
                    if (Mathf.Abs(z) < 0.55f)
                        continue; // sally port
                    float jx = Range(rng, -0.10f, 0.10f);
                    float jy = Range(rng, -0.02f, 0.02f);
                    AddBox(group.transform, "Sandbag", sandbag,
                        new Vector3(x0 + jx, GroundHeight(x0, z) + y + jy, z),
                        new Vector3(0f, Range(rng, -7f, 7f), Range(rng, -3f, 3f)),
                        new Vector3(0.46f * Range(rng, 0.9f, 1.1f), 0.32f, 0.60f * Range(rng, 0.9f, 1.08f)));
                }
            }

            // Timber revetment posts behind the bags.
            for (float z = -5.8f; z <= 5.9f; z += 2.3f)
            {
                float x = x0 + side * 0.55f;
                AddCylinder(group.transform, "Timber_Post", timber,
                    new Vector3(x + Range(rng, -0.1f, 0.1f), GroundHeight(x, z) + 0.52f, z + Range(rng, -0.3f, 0.3f)),
                    new Vector3(Range(rng, -4f, 4f), 0f, Range(rng, -4f, 4f)),
                    new Vector3(0.09f, 0.60f, 0.09f));
            }

            // Duckboard walkway sections behind the parapet.
            float xw = side * 18.2f;
            foreach (float zc in new[] { -3.4f, 0f, 3.4f })
            {
                float yaw = Range(rng, -4f, 4f);
                for (int p = 0; p < 10; p++)
                {
                    float z = zc - 1.08f + p * 0.24f;
                    AddBox(group.transform, "Duckboard", timber,
                        new Vector3(xw, GroundHeight(xw, z) + 0.05f, z),
                        new Vector3(0f, yaw, 0f),
                        new Vector3(1.25f, 0.04f, 0.20f));
                }
            }

            // Supply crate dumps near the trench ends.
            foreach (float zc in new[] { -4.6f, 4.4f })
            {
                float xc = side * Range(rng, 17.6f, 18.3f);
                float h = GroundHeight(xc, zc);
                AddBox(group.transform, "Crate", crate,
                    new Vector3(xc, h + 0.28f, zc), new Vector3(0f, Range(rng, 0f, 90f), 0f),
                    Vector3.one * 0.56f);
                AddBox(group.transform, "Crate", crate,
                    new Vector3(xc + Range(rng, -0.7f, 0.7f), h + 0.26f, zc + 0.65f),
                    new Vector3(0f, Range(rng, 0f, 90f), 0f), Vector3.one * 0.52f);
                AddBox(group.transform, "Crate", crate,
                    new Vector3(xc, h + 0.82f, zc + 0.2f), new Vector3(0f, Range(rng, 15f, 40f), 0f),
                    Vector3.one * 0.48f);

                // Fuel/water barrels beside the crate dump (one standing, one toppled).
                AddCylinder(group.transform, "Barrel", barrel,
                    new Vector3(xc - 0.85f, h + 0.34f, zc - 0.55f),
                    new Vector3(Range(rng, -4f, 4f), 0f, Range(rng, -4f, 4f)),
                    new Vector3(0.42f, 0.34f, 0.42f));
                AddCylinder(group.transform, "Barrel_Toppled", barrel,
                    new Vector3(xc - 0.4f, h + 0.22f, zc + 1.3f),
                    new Vector3(90f, Range(rng, 0f, 180f), 0f),
                    new Vector3(0.42f, 0.34f, 0.42f));
            }
        }

        /// <summary>Anti-tank hedgehogs (three crossed steel beams) strung along the
        /// wire belts — spec's Trenchline prop set. All outside the flat combat strip.</summary>
        private static void BuildTankTraps(Transform parent, Material steel, System.Random rng)
        {
            var group = new GameObject("TankTraps");
            group.transform.SetParent(parent, false);

            // Far belt (visible band behind the enemy-side wire) + a couple mid-ground.
            (float x, float z, float s)[] traps =
            {
                (-14.5f, 9.4f, 1.0f), (-8f, 8.8f, 1.1f), (-1.5f, 9.6f, 0.95f),
                (5f, 8.9f, 1.05f), (11.5f, 9.3f, 1.0f), (16f, 8.6f, 1.1f),
                (-19f, 13f, 1.2f), (20f, 12f, 1.15f),
                // Near-side pair (home frame crops these; punch-ins/scene view see them).
                (-10f, -8.9f, 1.05f), (9f, -9.2f, 1.0f),
            };
            foreach (var (x, z, s) in traps)
            {
                float h = GroundHeight(x, z);
                var trap = new GameObject("Hedgehog");
                trap.transform.SetParent(group.transform, false);
                trap.transform.position = new Vector3(x, h, z);
                trap.transform.rotation = Quaternion.Euler(0f, Range(rng, 0f, 120f), 0f);

                // Three beams crossing at ~0.45 m, classic Czech-hedgehog star.
                float beamLen = 1.5f * s;
                var beamScale = new Vector3(0.11f * s, 0.11f * s, beamLen);
                AddBox(trap.transform, "Beam", steel,
                    trap.transform.position + Vector3.up * (0.45f * s),
                    new Vector3(52f, 0f, 0f) + trap.transform.eulerAngles, beamScale);
                AddBox(trap.transform, "Beam", steel,
                    trap.transform.position + Vector3.up * (0.45f * s),
                    new Vector3(-48f, 122f, 0f) + trap.transform.eulerAngles, beamScale);
                AddBox(trap.transform, "Beam", steel,
                    trap.transform.position + Vector3.up * (0.45f * s),
                    new Vector3(50f, 243f, 0f) + trap.transform.eulerAngles, beamScale);
            }
        }

        // ------------------------------------------------------------------ wire + scatter

        private static void BuildWireLines(Transform parent, Material timber, Material wire, System.Random rng)
        {
            var group = new GameObject("BarbedWire");
            group.transform.SetParent(parent, false);
            BuildWireLine(group.transform, timber, wire, rng, z: 7.7f, xMin: -17f, xMax: 17f, step: 2.3f);
            BuildWireLine(group.transform, timber, wire, rng, z: -7.6f, xMin: -14f, xMax: 14f, step: 3.1f);
        }

        private static void BuildWireLine(
            Transform parent, Material timber, Material wire, System.Random rng,
            float z, float xMin, float xMax, float step)
        {
            var tops = new List<Vector3>();
            for (float x = xMin; x <= xMax + 0.01f; x += step)
            {
                float px = x + Range(rng, -0.3f, 0.3f);
                float pz = z + Range(rng, -0.4f, 0.4f);
                float h = GroundHeight(px, pz);
                AddCylinder(parent, "Wire_Post", timber,
                    new Vector3(px, h + 0.48f, pz),
                    new Vector3(Range(rng, -10f, 10f), 0f, Range(rng, -10f, 10f)),
                    new Vector3(0.055f, 0.50f, 0.055f));
                tops.Add(new Vector3(px, h, pz));
            }

            foreach (float strandY in new[] { 0.36f, 0.62f })
            {
                for (int i = 0; i + 1 < tops.Count; i++)
                {
                    var a = tops[i] + Vector3.up * strandY;
                    var b = tops[i + 1] + Vector3.up * (strandY + Range(rng, -0.05f, 0.05f));
                    var mid = (a + b) * 0.5f;
                    float len = Vector3.Distance(a, b);
                    var strand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    strand.name = "Wire_Strand";
                    Object.DestroyImmediate(strand.GetComponent<Collider>());
                    strand.transform.SetParent(parent, false);
                    strand.transform.position = mid;
                    strand.transform.rotation = Quaternion.LookRotation(b - a);
                    strand.transform.localScale = new Vector3(0.016f, 0.016f, len);
                    strand.GetComponent<MeshRenderer>().sharedMaterial = wire;
                }
            }
        }

        private static void BuildMidgroundScatter(Transform parent, Material timber, System.Random rng)
        {
            var group = new GameObject("Scatter");
            group.transform.SetParent(parent, false);

            // Leaning telegraph poles in the visible mid-ground band.
            if (_p.TelegraphPoles)
            {
                foreach (var (x, z, lean) in new[] { (-12f, 13f, 8f), (3f, 17f, -12f), (18f, 12f, 6f) })
                {
                    float h = GroundHeight(x, z);
                    var pole = new GameObject("Telegraph_Pole");
                    pole.transform.SetParent(group.transform, false);
                    pole.transform.position = new Vector3(x, h, z);
                    pole.transform.rotation = Quaternion.Euler(lean, Range(rng, 0f, 180f), 0f);
                    AddCylinder(pole.transform, "Pole", timber,
                        new Vector3(x, h, z) + pole.transform.up * 1.6f, pole.transform.eulerAngles,
                        new Vector3(0.07f, 1.6f, 0.07f));
                    AddBox(pole.transform, "Crossarm", timber,
                        new Vector3(x, h, z) + pole.transform.up * 2.85f, pole.transform.eulerAngles,
                        new Vector3(0.9f, 0.07f, 0.07f));
                }
            }

            // Debris planks near far-side craters (picks skip indexes past a theme's
            // smaller crater field — the pick list was authored on Trenchline's 14).
            int[] craterPicks = { 0, 1, 2, 5, 6, 8, 9, 12, 13 };
            foreach (int ci in craterPicks)
            {
                if (ci >= _p.Craters.Length)
                    continue;
                var c = _p.Craters[ci];
                float ang = Range(rng, 0f, Mathf.PI * 2f);
                float d = c.z * Range(rng, 1.15f, 1.7f);
                float x = c.x + Mathf.Cos(ang) * d;
                float z = c.y + Mathf.Sin(ang) * d;
                // Seated slightly INTO the ground: crater rims slope, and a plank floating
                // above its own shadow reads instantly fake; half-buried reads as debris.
                AddBox(group.transform, "Debris_Plank", timber,
                    new Vector3(x, GroundHeight(x, z) - 0.01f, z),
                    new Vector3(Range(rng, -14f, 14f), Range(rng, 0f, 180f), Range(rng, -10f, 10f)),
                    new Vector3(Range(rng, 0.9f, 1.6f), 0.05f, Range(rng, 0.16f, 0.26f)));
            }
        }

        // ------------------------------------------------------------------ backdrop

        private static void BuildBackdrop(Transform parent, Material backdrop, System.Random rng)
        {
            var group = new GameObject("Backdrop");
            group.transform.SetParent(parent, false);

            var ridgeMesh = BuildRidgeMesh();

            // Ridge ring: bearings measured from +z, fog does the depth fade. The camera
            // sits only 8.5 m up, so silhouettes must live inside the visible ground band
            // (z < ~40) and be tall enough to break the frame's top edge as a ridge line.
            (float bearing, float radius, float scale)[] ridges =
            {
                (-125f, 34f, 1.6f), (-95f, 40f, 1.4f), (-62f, 42f, 1.8f), (-34f, 44f, 1.7f),
                (-12f, 40f, 2.1f), (12f, 43f, 1.8f), (36f, 41f, 2.0f), (64f, 42f, 1.6f),
                (92f, 38f, 1.5f), (120f, 34f, 1.6f),
            };
            foreach (var (bearing, radius, scale) in ridges)
            {
                float rad = bearing * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
                pos.y = GroundHeight(pos.x, pos.z) - 0.5f;
                var ridge = new GameObject("Ridge");
                ridge.transform.SetParent(group.transform, false);
                ridge.transform.position = pos;
                ridge.transform.rotation = Quaternion.Euler(0f, bearing + 90f + Range(rng, -14f, 14f), 0f);
                ridge.transform.localScale = new Vector3(scale, scale * Range(rng, 0.8f, 1.2f), scale);
                ridge.AddComponent<MeshFilter>().sharedMesh = ridgeMesh;
                ridge.AddComponent<MeshRenderer>().sharedMaterial = backdrop;
            }

            // Mid-distance ruin masses INSIDE the visible band (the camera's frame-top ray
            // hits ground at z~29, so these are what actually anchors the home-frame horizon;
            // they crop off the top edge as dark battered shapes in ~35% fog).
            foreach (var (x, z, h0) in new[] { (-18f, 22f, 3.4f), (7f, 25f, 4.0f), (21f, 19f, 3.0f) })
            {
                float gh = GroundHeight(x, z);

                // Rubble mound (small ridge-mesh instance — jagged, not a monolith box).
                var mound = new GameObject("Ruin_RubbleMound");
                mound.transform.SetParent(group.transform, false);
                mound.transform.position = new Vector3(x, gh - 0.3f, z);
                mound.transform.rotation = Quaternion.Euler(0f, Range(rng, 0f, 180f), 0f);
                mound.transform.localScale = new Vector3(0.32f, Range(rng, 0.35f, 0.5f), 0.30f);
                mound.AddComponent<MeshFilter>().sharedMesh = ridgeMesh;
                mound.AddComponent<MeshRenderer>().sharedMaterial = backdrop;

                // Broken wall slabs — thin, tilted, half-buried.
                for (int w = 0; w < 2; w++)
                {
                    AddBox(group.transform, "Ruin_WallSlab", backdrop,
                        new Vector3(x + Range(rng, -3f, 3f), gh + h0 * 0.5f - 0.7f, z + Range(rng, -1.5f, 1.5f)),
                        new Vector3(Range(rng, -5f, 5f), Range(rng, -40f, 40f), Range(rng, 5f, 14f)),
                        new Vector3(Range(rng, 2.6f, 4.2f), h0 * Range(rng, 0.8f, 1.1f), 0.45f));
                }

                AddBox(group.transform, "Ruin_Chimney", backdrop,
                    new Vector3(x + Range(rng, 1.5f, 2.8f), gh + 1.8f, z + Range(rng, -1f, 1f)),
                    new Vector3(0f, Range(rng, 0f, 45f), Range(rng, -4f, 4f)),
                    new Vector3(0.8f, Range(rng, 3.4f, 4.6f), 0.8f));
            }

            // Ruined-structure silhouettes farther out (punch-in / scene-view dressing).
            foreach (var (x, z) in new[] { (-27f, 28f), (18f, 34f), (32f, 16f), (-5f, 38f) })
            {
                float h = GroundHeight(x, z);
                float mainH = Range(rng, 6f, 9f);
                AddBox(group.transform, "Ruin_Shell", backdrop,
                    new Vector3(x, h + mainH * 0.5f - 0.6f, z),
                    new Vector3(0f, Range(rng, 0f, 90f), 0f),
                    new Vector3(Range(rng, 6f, 9f), mainH, Range(rng, 4f, 6f)));
                AddBox(group.transform, "Ruin_Annex", backdrop,
                    new Vector3(x + Range(rng, 3f, 6f), h + 2.4f, z + Range(rng, -2f, 2f)),
                    new Vector3(0f, Range(rng, 0f, 90f), Range(rng, 3f, 9f)),
                    new Vector3(4.5f, 5f, 4.5f));
                AddBox(group.transform, "Ruin_Chimney", backdrop,
                    new Vector3(x - Range(rng, 2f, 4f), h + 5f, z + Range(rng, -1.5f, 1.5f)),
                    Vector3.zero, new Vector3(0.9f, Range(rng, 9f, 11f), 0.9f));
            }
        }

        /// <summary>Cached per Build(): SaveAsset delete-and-recreates, so a second call
        /// in the same build would orphan every earlier instance's mesh reference.</summary>
        private static Mesh _ridgeMesh;

        private static Mesh BuildRidgeMesh()
        {
            if (_ridgeMesh != null)
                return _ridgeMesh;
            const int segs = 26;
            float[] cross = { -9f, -4.2f, 0f, 4.2f, 9f };
            float[] heightFactor = { 0f, 0.55f, 1f, 0.55f, 0f };

            var verts = new Vector3[(segs + 1) * cross.Length];
            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                float z = (t - 0.5f) * 46f;
                // Max() guards Pow against float sin(pi) being slightly negative (NaN).
                float env = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI)), 0.55f);
                float h = (3.4f + 3.2f * ValueNoise(i * 0.37f + 3.1f, 8.8f)) * env;
                float xJit = (ValueNoise(i * 0.29f + 11f, 2.2f) - 0.5f) * 3f;
                for (int j = 0; j < cross.Length; j++)
                    verts[i * cross.Length + j] = new Vector3(
                        cross[j] + xJit * heightFactor[j], h * heightFactor[j], z);
            }

            var tris = new int[segs * (cross.Length - 1) * 6];
            int t2 = 0;
            for (int i = 0; i < segs; i++)
            for (int j = 0; j < cross.Length - 1; j++)
            {
                int v = i * cross.Length + j;
                tris[t2++] = v; tris[t2++] = v + 1; tris[t2++] = v + cross.Length;
                tris[t2++] = v + 1; tris[t2++] = v + cross.Length + 1; tris[t2++] = v + cross.Length;
            }

            var mesh = new Mesh { name = "Combat3D_RidgeMesh", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            SaveAsset(mesh, RidgeMeshPath);
            _ridgeMesh = mesh;
            return mesh;
        }

        // ------------------------------------------------------------------ theme extras

        /// <summary>Fog Field: burnt stakes, collapsed fence runs, and a pair of
        /// abandoned hedgehogs — remnants dissolving into the murk. Sparse on purpose:
        /// the fog is the theme; props only give it depth cues.</summary>
        private static void BuildFogFieldRemnants(
            Transform parent, Material timber, Material steel, System.Random rng)
        {
            var group = new GameObject("FogRemnants");
            group.transform.SetParent(parent, false);

            // Burnt stakes: lone tilted posts at staggered depths (fog reads distance
            // off them). All well outside the flat strip.
            (float x, float z)[] stakes =
            {
                (-13f, 9.5f), (-6f, 12f), (1f, 9f), (7.5f, 13.5f), (14f, 10f),
                (-19f, 16f), (10f, 19f), (-3f, 22f), (19f, 15f), (-10f, 27f),
                (-20f, -9f), (-4f, -10f), (9f, -9.5f), (21f, -8.5f),
            };
            foreach (var (x, z) in stakes)
            {
                float h = GroundHeight(x, z);
                AddCylinder(group.transform, "Burnt_Stake", timber,
                    new Vector3(x + Range(rng, -0.4f, 0.4f), h + Range(rng, 0.35f, 0.6f), z + Range(rng, -0.4f, 0.4f)),
                    new Vector3(Range(rng, -14f, 14f), Range(rng, 0f, 180f), Range(rng, -14f, 14f)),
                    new Vector3(Range(rng, 0.07f, 0.12f), Range(rng, 0.45f, 0.85f), Range(rng, 0.07f, 0.12f)));
            }

            // Collapsed fence runs: two posts + a fallen rail.
            foreach (var (x, z, yaw) in new[] { (-16f, 12f, 20f), (5f, 16f, -35f), (17f, -9f, 70f) })
            {
                float h = GroundHeight(x, z);
                AddCylinder(group.transform, "Fence_Post", timber,
                    new Vector3(x, h + 0.4f, z), new Vector3(Range(rng, -8f, 8f), yaw, Range(rng, -8f, 8f)),
                    new Vector3(0.06f, 0.42f, 0.06f));
                AddCylinder(group.transform, "Fence_Post", timber,
                    new Vector3(x + 1.6f, h + 0.28f, z + 0.3f), new Vector3(Range(rng, 15f, 30f), yaw, 0f),
                    new Vector3(0.06f, 0.34f, 0.06f));
                AddBox(group.transform, "Fence_Rail", timber,
                    new Vector3(x + 0.8f, h + 0.12f, z + 0.2f),
                    new Vector3(Range(rng, -6f, 6f), yaw + 90f, Range(rng, -8f, 8f)),
                    new Vector3(0.07f, 0.07f, 2.1f));
            }

            // Two hedgehogs half-lost in the fog band (steel remnants, not a defense line).
            foreach (var (x, z) in new[] { (-8f, 15f), (12f, 16.5f) })
            {
                float h = GroundHeight(x, z);
                var beamScale = new Vector3(0.10f, 0.10f, 1.4f);
                AddBox(group.transform, "Beam", steel, new Vector3(x, h + 0.4f, z),
                    new Vector3(52f, Range(rng, 0f, 120f), 0f), beamScale);
                AddBox(group.transform, "Beam", steel, new Vector3(x, h + 0.4f, z),
                    new Vector3(-48f, Range(rng, 120f, 240f), 0f), beamScale);
            }
        }

        /// <summary>Ravaged Town: a shelled streetfront ACROSS the far visible band (the
        /// camera's wedge never sees the strip's flanks — Trenchline's trenchworks taught
        /// the same lesson), rubble at its feet, sandbag barricades, gutted blocks behind.
        /// The strip itself stays an open road — combat readability owns it.</summary>
        private static void BuildTownRuins(
            Transform parent, Material backdrop, Material timber, Material sandbag, System.Random rng)
        {
            var group = new GameObject("TownRuins");
            group.transform.SetParent(parent, false);
            var ridgeMesh = BuildRidgeMesh();

            // Streetfront: battered facades facing the camera along z ~10.5-13, with a
            // central breach (|x| < 3 stays empty — the eye exits the street there).
            // Backdrop (Lit) material: fog fades them, and the fullscreen edge pass owns
            // their line work.
            foreach (var (x, w, hgt) in new[]
            {
                (-19f, 4.2f, 4.6f), (-13.5f, 3.4f, 3.4f), (-8f, 4.0f, 5.4f), (-4.2f, 2.2f, 2.6f),
                (4.5f, 2.6f, 3.0f), (8.5f, 4.2f, 5.0f), (14f, 3.6f, 3.8f), (19.5f, 4.0f, 4.4f),
            })
            {
                float z = Range(rng, 10.5f, 13f);
                float gh = GroundHeight(x, z);
                AddBox(group.transform, "Facade_Wall", backdrop,
                    new Vector3(x + Range(rng, -0.4f, 0.4f), gh + hgt * 0.5f - 0.4f, z),
                    new Vector3(Range(rng, -2f, 2f), Range(rng, -10f, 10f), Range(rng, -3f, 3f)),
                    new Vector3(w, hgt, 0.5f));
                // Second storey fragment on the taller shells.
                if (hgt > 4f)
                    AddBox(group.transform, "Facade_Fragment", backdrop,
                        new Vector3(x + Range(rng, -0.8f, 0.8f), gh + hgt + 0.6f, z + Range(rng, -0.3f, 0.3f)),
                        new Vector3(Range(rng, -6f, 6f), Range(rng, -12f, 12f), Range(rng, -8f, 8f)),
                        new Vector3(w * Range(rng, 0.3f, 0.55f), Range(rng, 0.9f, 1.7f), 0.45f));
                // Chimney stub against some facades.
                if (w > 3.5f)
                    AddBox(group.transform, "Facade_Chimney", backdrop,
                        new Vector3(x + Range(rng, -w * 0.3f, w * 0.3f), gh + hgt * 0.9f, z + 0.7f),
                        new Vector3(0f, Range(rng, 0f, 45f), Range(rng, -3f, 3f)),
                        new Vector3(0.65f, hgt * Range(rng, 1.3f, 1.6f), 0.65f));

                // Rubble spilling from the facade toward (never onto) the strip.
                float rz = z - Range(rng, 1.6f, 2.6f);
                var mound = new GameObject("Rubble_Mound");
                mound.transform.SetParent(group.transform, false);
                mound.transform.position = new Vector3(x + Range(rng, -1f, 1f), GroundHeight(x, rz) - 0.25f, rz);
                mound.transform.rotation = Quaternion.Euler(0f, Range(rng, 60f, 120f), 0f);
                mound.transform.localScale = new Vector3(Range(rng, 0.07f, 0.11f), Range(rng, 0.09f, 0.15f), 0.08f);
                mound.AddComponent<MeshFilter>().sharedMesh = ridgeMesh;
                mound.AddComponent<MeshRenderer>().sharedMaterial = backdrop;
            }

            // Near-side wall stubs (home frame crops them; punch-ins see them).
            foreach (var (x, z) in new[] { (-12f, -9.2f), (3f, -9.8f), (14f, -9f) })
            {
                float gh = GroundHeight(x, z);
                AddBox(group.transform, "Wall_Stub", backdrop,
                    new Vector3(x, gh + Range(rng, 0.7f, 1.2f), z),
                    new Vector3(Range(rng, -4f, 4f), Range(rng, 0f, 180f), Range(rng, -6f, 6f)),
                    new Vector3(Range(rng, 2.2f, 3.6f), Range(rng, 1.6f, 2.6f), 0.45f));
            }

            // Street barricades: sandbag stacks + a timber beam (muted palette — the v1
            // crate stacks were the most saturated thing on screen, which is VFX budget).
            foreach (var (x, z) in new[] { (-11f, 9f), (12.5f, 9.3f), (-6f, -8.8f), (8f, -9f) })
            {
                float h = GroundHeight(x, z);
                for (int b = 0; b < 4; b++)
                    AddBox(group.transform, "Barricade_Sandbag", sandbag,
                        new Vector3(x + (b % 2) * 0.5f - 0.25f + Range(rng, -0.06f, 0.06f),
                            h + 0.16f + (b / 2) * 0.29f, z + Range(rng, -0.1f, 0.1f)),
                        new Vector3(0f, Range(rng, -8f, 8f), Range(rng, -3f, 3f)),
                        new Vector3(0.5f * Range(rng, 0.9f, 1.1f), 0.3f, 0.62f));
                AddBox(group.transform, "Barricade_Beam", timber,
                    new Vector3(x + 0.2f, h + 0.75f, z),
                    new Vector3(Range(rng, -6f, 6f), Range(rng, 20f, 70f), Range(rng, -10f, 10f)),
                    new Vector3(0.12f, 0.12f, 2.2f));
            }

            // Gutted blocks behind the streetfront — the town has depth, not one wall.
            foreach (var (x, z) in new[] { (-22f, 19f), (-9f, 21f), (5f, 18.5f), (16f, 22f), (25f, 17f) })
            {
                float h = GroundHeight(x, z);
                float mainH = Range(rng, 5f, 7.5f);
                AddBox(group.transform, "Block_Shell", backdrop,
                    new Vector3(x, h + mainH * 0.5f - 0.5f, z), new Vector3(0f, Range(rng, 0f, 90f), 0f),
                    new Vector3(Range(rng, 4.5f, 6.5f), mainH, Range(rng, 3.5f, 5f)));
                AddBox(group.transform, "Block_Chimney", backdrop,
                    new Vector3(x + Range(rng, 1.5f, 3f), h + mainH * 0.75f, z + Range(rng, -1f, 1f)),
                    new Vector3(0f, Range(rng, 0f, 45f), Range(rng, -3f, 3f)),
                    new Vector3(0.7f, mainH * 1.5f, 0.7f));
            }
        }

        /// <summary>Wartorn Forest: shattered standing trunks with splintered crowns,
        /// stumps and fallen logs, and a fog-faded tree-line ring for the horizon.
        /// Trunks keep clear of the strip so unit silhouettes never fight them.</summary>
        private static void BuildForestDeadfall(
            Transform parent, Material timber, Material backdrop, System.Random rng)
        {
            var group = new GameObject("ForestDeadfall");
            group.transform.SetParent(parent, false);

            // Standing shattered trunks (visible band + flanks).
            (float x, float z)[] trunks =
            {
                (-14f, 10f), (-7f, 13f), (-1.5f, 10.5f), (5f, 15f), (11f, 9.5f), (16.5f, 13f),
                (-20f, 19f), (-12f, 24f), (2f, 26f), (9f, 21f), (20f, 18f), (-26f, 9f), (25f, 7f),
                (-18f, -9f), (-8f, -10f), (4f, -9.2f), (13f, -10f), (22f, -8.5f),
            };
            foreach (var (x, z) in trunks)
            {
                float h = GroundHeight(x, z);
                // Fat and broken-short: v1's 0.12-0.24 radius at full height read as
                // telegraph poles, not shattered trees (cylinder scale y = half-height).
                float trunkH = Range(rng, 0.9f, 2.2f);
                float radius = Range(rng, 0.28f, 0.46f);
                float leanX = Range(rng, -7f, 7f);
                float leanZ = Range(rng, -7f, 7f);
                AddCylinder(group.transform, "Shattered_Trunk", timber,
                    new Vector3(x, h + trunkH, z), new Vector3(leanX, Range(rng, 0f, 180f), leanZ),
                    new Vector3(radius, trunkH, radius));
                // Splintered crown: jagged shards past the break, sized to read at range.
                for (int s = 0; s < 3; s++)
                    AddBox(group.transform, "Splinter", timber,
                        new Vector3(x + Range(rng, -0.25f, 0.25f), h + trunkH * 2f + Range(rng, 0.1f, 0.5f), z + Range(rng, -0.25f, 0.25f)),
                        new Vector3(Range(rng, -30f, 30f), Range(rng, 0f, 180f), Range(rng, -30f, 30f)),
                        new Vector3(Range(rng, 0.09f, 0.16f), Range(rng, 0.5f, 1.1f), Range(rng, 0.09f, 0.16f)));
            }

            // Stumps and fallen logs in the same band.
            foreach (var (x, z) in new[] { (-10f, 8.8f), (7f, 11f), (18f, 16f), (-16f, 15f), (0f, -9.5f), (15f, -9f) })
            {
                float h = GroundHeight(x, z);
                AddCylinder(group.transform, "Stump", timber,
                    new Vector3(x, h + Range(rng, 0.18f, 0.32f), z),
                    new Vector3(Range(rng, -5f, 5f), 0f, Range(rng, -5f, 5f)),
                    new Vector3(Range(rng, 0.2f, 0.32f), Range(rng, 0.2f, 0.35f), Range(rng, 0.2f, 0.32f)));
                AddCylinder(group.transform, "Fallen_Log", timber,
                    new Vector3(x + Range(rng, 0.8f, 1.8f), h + 0.2f, z + Range(rng, -0.8f, 0.8f)),
                    new Vector3(90f + Range(rng, -6f, 6f), Range(rng, 0f, 180f), 0f),
                    new Vector3(0.22f, Range(rng, 1.2f, 2.2f), 0.22f));
            }

            // Tree-line horizon: dark canopy masses INSIDE the visible band (the frame-top
            // ray lands at ground z~36; v1's ring at 33-42 was cropped to nothing). Own
            // green-grey Lit material — fog fades it into a ragged silhouette (single
            // grey boxes read as distant RUINS, not trees); doctrine same as ridges.
            var canopyMat = EnsureLitMaterial(
                _p.MaterialPath("Canopy"), new Color(0.105f, 0.125f, 0.095f));
            for (int i = 0; i < 12; i++)
            {
                float bearing = -72f + i * 13f + Range(rng, -4f, 4f);
                float radius = Range(rng, 24f, 33f);
                float rad = bearing * Mathf.Deg2Rad;
                float x = Mathf.Sin(rad) * radius;
                float z = Mathf.Cos(rad) * radius;
                float h = GroundHeight(x, z);
                float treeH = Range(rng, 4.5f, 7f);
                AddCylinder(group.transform, "TreeLine_Trunk", backdrop,
                    new Vector3(x, h + treeH * 0.5f - 0.4f, z),
                    new Vector3(Range(rng, -4f, 4f), 0f, Range(rng, -4f, 4f)),
                    new Vector3(Range(rng, 0.6f, 1.0f), treeH * 0.5f, Range(rng, 0.6f, 1.0f)));
                // Ragged crown: a wide low mass + a smaller offset cap, both tilted.
                AddBox(group.transform, "TreeLine_Canopy", canopyMat,
                    new Vector3(x + Range(rng, -0.8f, 0.8f), h + treeH * Range(rng, 0.65f, 0.8f), z),
                    new Vector3(Range(rng, -12f, 12f), Range(rng, 0f, 90f), Range(rng, -12f, 12f)),
                    new Vector3(Range(rng, 3.2f, 5.2f), Range(rng, 1.6f, 2.4f), Range(rng, 2.6f, 4.2f)));
                AddBox(group.transform, "TreeLine_CanopyCap", canopyMat,
                    new Vector3(x + Range(rng, -1.2f, 1.2f), h + treeH * Range(rng, 0.9f, 1.05f), z + Range(rng, -0.8f, 0.8f)),
                    new Vector3(Range(rng, -18f, 18f), Range(rng, 0f, 90f), Range(rng, -18f, 18f)),
                    new Vector3(Range(rng, 1.6f, 2.8f), Range(rng, 1.0f, 1.8f), Range(rng, 1.4f, 2.4f)));
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void AssertLanesFlat()
        {
            for (float x = -15.3f; x <= 15.3f; x += 0.9f)
            for (float z = -5.4f; z <= 5.4f; z += 0.9f)
            {
                if (Mathf.Abs(GroundHeight(x, z)) > 0.0005f)
                {
                    Debug.LogError($"[Combat3D] Environment self-check FAILED: ground height " +
                                   $"{GroundHeight(x, z):F3} at ({x:F1}, {z:F1}) — unit grounding (y=0) broken.");
                    return;
                }
            }
        }

        private static void AddBox(Transform parent, string name, Material mat,
            Vector3 pos, Vector3 euler, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void AddCylinder(Transform parent, string name, Material mat,
            Vector3 pos, Vector3 euler, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static Material EnsureLitMaterial(string path, Color baseColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureToonInkMaterial(string path, Color baseColor, float outline)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("DMZ/ToonInk");
            if (shader == null)
            {
                Debug.LogError("[Combat3D] DMZ/ToonInk shader not found — environment material falls back to URP Lit.");
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            // Hull outline doctrine: ON for clean-normal primitive env meshes (crisp ink line);
            // OFF (0) where the fullscreen edge pass should own the line (wire, backdrop).
            if (material.HasProperty("_OutlineWidth"))
                material.SetFloat("_OutlineWidth", outline);
            if (material.HasProperty("_ShadowColor"))
                material.SetColor("_ShadowColor", baseColor * 0.55f);
            if (material.HasProperty("_InkStrength"))
                material.SetFloat("_InkStrength", 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void MarkStatic(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        private static void SaveAsset(Object asset, string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        private static float Range(System.Random rng, float min, float max) =>
            min + (float)rng.NextDouble() * (max - min);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // Deterministic hash-lattice value noise (no UnityEngine.Random, no state).
        private static float Hash01(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ 0x9E3779B9u;
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        private static float ValueNoise(float x, float y)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash01(x0, y0), b = Hash01(x0 + 1, y0);
            float c = Hash01(x0, y0 + 1), d = Hash01(x0 + 1, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }
    }
}
#endif
