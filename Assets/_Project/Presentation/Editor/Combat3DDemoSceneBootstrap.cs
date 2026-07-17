#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Builds Assets/_Project/Scenes/Combat3D_Demo.unity: spike-look lighting (warm key /
    /// cool ambient, P0_Grade volume), the Trenchline battlefield environment
    /// (CombatEnvironmentBuilder — broken earth, trenchworks, wire, backdrop ring), and the
    /// combat arena rig (bootstrap/director/presenter/pool) configured for ToonInk3D visuals
    /// with the Phase-0 rifleman model, a freshly generated AnimatorController, and side rings.
    /// The spike scene/controller stay throwaway — everything generated lands under _Project.
    /// </summary>
    public static class Combat3DDemoSceneBootstrap
    {
        private const string ScenePath = "Assets/_Project/Scenes/Combat3D_Demo.unity";
        private const string GeneratedFolder = "Assets/_Project/Combat3D";
        private const string ControllerPath = GeneratedFolder + "/RiflemanCombat3D.controller";
        private const string IdleClipPath = GeneratedFolder + "/Rifleman_Idle.anim";
        private const string WalkClipPath = GeneratedFolder + "/Rifleman_Walk.anim";
        private const string DieClipPath = GeneratedFolder + "/Rifleman_Die.anim";
        private const string ConfigPath = GeneratedFolder + "/CombatArena3DDemoConfig.asset";
        private const string CombatRendererPath = "Assets/_Project/Settings/Rendering/DeadManZone_CombatRenderer.asset";

        // Roster archetypes: Meshy 12k units copied under _Project (idle/walk/die GLBs sharing
        // one rig per unit), mapped to the ContentDatabase piece id the sim uses.
        // A unit with broken/missing GLBs is skipped with a warning - actors with its piece id
        // fall back to the default rifleman visuals instead of blocking the scene build.
        //
        // 2026-07-15 faction-roster-v1 (comic-noir §7.2 regen): every combat-eligible
        // humanoid piece has its own model folder under Models/<piece_id>/ from
        // run_roster_batch.ps1. conscript_rifles is generated from the canonical
        // s09_comic_noir ref. Any piece missing its GLBs falls back to the default
        // rifleman visuals (IdleGlbPath etc.) until its chain lands.
        private static readonly (string folder, string pieceId)[] RosterUnits =
        {
            // neutral
            ("militia_squad", "militia_squad"),
            ("field_medic", "field_medic"),
            // ironmarch union
            ("conscript_rifles", "conscript_rifles"),
            ("line_grenadiers", "line_grenadiers"),
            ("field_mortar_team", "field_mortar_team"),
            ("sharpshooter", "sharpshooter"),
            ("iron_guard", "iron_guard"),
            ("forward_observer", "forward_observer"),
            ("shock_sergeant", "shock_sergeant"),
            // marksman_doctrine_officer has no model folder of its own (never got a
            // §7.2 regen pass) — it's picked up by BuildHumanoidReuseArchetypes below
            // via its sniper combatRole, same as every other faction's modelless humanoid.
        };

        // 2026-07-17 Wave 3 (temp-art pass, faction-roster-v1 §11): every faction's roster
        // beyond IronMarch/neutral still has zero dedicated models. Rather than hand-list
        // ~55 (folder, pieceId) tuples, every modelless combat-eligible humanoid piece
        // (PieceCategory.Unit, primary "infantry") is assigned the closest-role existing
        // model by its combatRole tag. One controller/clip set is built PER FOLDER (not per
        // piece) and shared across every piece mapped to it — building it per-piece would
        // re-run SaveClipCopy/CreateAnimatorControllerAtPath at the same asset path for each
        // piece sharing a folder, which DELETES+RECREATES that path's assets every time and
        // invalidates the C# object references already handed to earlier archetypes in the
        // same build (Unity replaces the object behind the path on CreateAsset/DeleteAsset).
        private static readonly Dictionary<string, string> HumanoidReuseFolderByCombatRole = new()
        {
            ["assault"] = "conscript_rifles",   // line infantry — rifle-assault posture
            ["defender"] = "iron_guard",         // heavy defender body
            ["sniper"] = "sharpshooter",
            ["artillery"] = "field_mortar_team", // crew-served weapon team
            ["gas"] = "conscript_rifles",        // attacker posture closest to assault
            ["support"] = "field_medic",
            ["utility"] = "shock_sergeant",      // command/officer archetype
        };
        private const string HumanoidReuseDefaultFolder = "conscript_rifles";

        // Non-humanoid units (owner spec, audit "non-humanoid treatment"): single static
        // model.glb from generate_unit.py --vehicle, rendered by CombatUnitVisual3DVehicle
        // (code-driven rumble/recoil/collapse — no rig, no clips, no rifle). Height =
        // authored silhouette height in meters (these are not 1.7 m infantry).
        // yaw = per-gen facing correction (Meshy side-view refs come out with arbitrary
        // authored forward); tuned live in Play mode, baked here.
        private static readonly (string folder, string pieceId, float height, float yaw)[] VehicleUnits =
        {
            // PROVISIONAL heights/yaws (roster-v1 comic-noir regen 2026-07-15): facing is
            // per-gen roulette — verify each via isolated probe in Play mode and bake the
            // corrected yaw here, same procedure as the retired iron_horse/transport gens.
            ("breakthrough_tank", "breakthrough_tank", 2.6f, -90f),
            ("grand_battery", "grand_battery", 3.0f, -90f),
            ("machine_gun_nest", "machine_gun_nest", 1.6f, 90f),
            ("trench_works", "trench_works", 1.2f, 0f),
        };

        /// <summary>Coarse silhouette family for a generated primitive fallback — decides the
        /// box's proportions and added greebles (turret+barrel / roof block / bare slab).</summary>
        private enum PrimitiveKind { Tank, Structure, Wall }

        // 2026-07-17 Wave 3: combat-eligible vehicles/structures with no Meshy model yet
        // (Meshy credits pending) get an outlined grey-box primitive instead of the rifleman
        // fallback. Heights are PROVISIONAL (owner call, same status as VehicleUnits' own
        // heights above) — tank/structure/wall are the three silhouette bands the temp-art
        // pass asked for. HQ-only buildings (PieceCategory.Building) never spawn in combat
        // and are excluded — this list is combat-board pieces only.
        private static readonly (string pieceId, int cellsX, int cellsY, PrimitiveKind kind, float height)[]
            PrimitiveFallbackUnits =
        {
            ("scout_tankette", 2, 1, PrimitiveKind.Tank, 2.4f),
            ("vanquisher_doctrine_tank", 2, 2, PrimitiveKind.Tank, 2.4f),
            ("stiller_suppression_platform", 2, 2, PrimitiveKind.Tank, 2.4f),
            ("armored_ark", 2, 2, PrimitiveKind.Structure, 1.6f),
            ("corpse_tithe_caravan", 3, 1, PrimitiveKind.Structure, 1.6f),
            ("perpetual_engine", 2, 2, PrimitiveKind.Structure, 1.6f),
            ("resonance_coil", 2, 1, PrimitiveKind.Structure, 1.6f),
            ("vitriol_throne", 2, 2, PrimitiveKind.Structure, 1.6f),
            ("bunker_emplacement", 2, 1, PrimitiveKind.Wall, 1.0f),
        };

        private static string RosterModelFolder(string unitFolder) =>
            GeneratedFolder + "/Models/" + unitFolder;

        // Default rifleman GLBs: _Project copies of the proven Phase-0 spike set
        // (enlisted_rifleman_12k*.glb), so the scene's models no longer depend on the
        // throwaway spike folder. Materials/grade below are still spike-sourced.
        private const string IdleGlbPath = GeneratedFolder + "/Models/enlisted_rifleman/idle.glb";
        private const string WalkGlbPath = GeneratedFolder + "/Models/enlisted_rifleman/walk.glb";
        private const string DieGlbPath = GeneratedFolder + "/Models/enlisted_rifleman/die.glb";

        // Proven Phase-0 spike assets (referenced, never modified).
        private const string PlayerMaterialPath = "Assets/_Phase0Spike/Materials/Unit_Player.mat";
        private const string EnemyMaterialPath = "Assets/_Phase0Spike/Materials/Unit_Enemy.mat";
        private const string FallbackUnitMaterialPath = "Assets/_Phase0Spike/Materials/ToonInk_Meshy.mat";
        private const string GradeProfilePath = "Assets/_Phase0Spike/Materials/P0_Grade.asset";

        // Ring-fill health rings (replace the spike's flat RingBlue/RingRed discs): the base
        // ring IS the unit health display. Fill colors sampled from the spike ring palette;
        // rims lifted slightly so a near-dead unit's side still reads. Muted per bible §3.
        private const string RingFillShaderPath =
            "Assets/_Project/Presentation/Combat/Arena/Shaders/CombatRingFill.shader";
        private const string PlayerRingPath = GeneratedFolder + "/RingFill_Player.mat";
        private const string EnemyRingPath = GeneratedFolder + "/RingFill_Enemy.mat";

        /// <summary>Everything a Combat3D scene build needs, generated/validated once.
        /// Shared by the demo scene and the run-flow CombatArena3D scene builders.</summary>
        /// <summary>One installer archetype entry: humanoid (model + controller) or vehicle
        /// (static model, code-driven motion, per-unit silhouette height).</summary>
        internal struct ArchetypeSpec
        {
            public string PieceId;
            public GameObject Model;
            public AnimatorController Controller;
            public bool IsVehicle;
            public float VehicleHeight;
            public float VehicleYawOffsetDegrees;
        }

        internal sealed class SceneAssets
        {
            public GameObject IdleModel;
            public Material PlayerMat;
            public Material EnemyMat;
            public Material RingBlue;
            public Material RingRed;
            public VolumeProfile GradeProfile;
            public AnimatorController Controller;
            public List<ArchetypeSpec> Archetypes;
            public CombatArenaConfigSO Config;
            public GameObject RiflePrefab;
            public CombatArenaAudioSetSO AudioSet;
            public AudioClip AmbienceLoop;
        }

        /// <summary>Validate the spike/model assets and (re)generate the derived assets
        /// (materials, controllers, config, rifle, audio). Null + logged error on failure.</summary>
        internal static SceneAssets PrepareSceneAssets()
        {
            // --- Validate spike assets up front; friendly abort if the spike moved. ---
            var missing = new List<string>();
            var idleModel = LoadOrFlag<GameObject>(IdleGlbPath, missing);
            LoadOrFlag<GameObject>(WalkGlbPath, missing);
            LoadOrFlag<GameObject>(DieGlbPath, missing);
            // Tuned _Project copies of the spike unit materials. The spike defaults
            // (_ShadowColor 0.10) read as solid black at gameplay camera distance;
            // these lift the shadow band so the toon-ink shading stays readable.
            var playerMat = LoadOrCreateTunedUnitMaterial(
                PlayerMaterialPath, GeneratedFolder + "/Unit_Player_3D.mat");
            var enemyMat = LoadOrCreateTunedUnitMaterial(
                EnemyMaterialPath, GeneratedFolder + "/Unit_Enemy_3D.mat");
            var fallbackMat = AssetDatabase.LoadAssetAtPath<Material>(FallbackUnitMaterialPath);
            if (playerMat == null)
                playerMat = fallbackMat;
            if (enemyMat == null)
                enemyMat = fallbackMat;
            if (playerMat == null || enemyMat == null)
                missing.Add($"{PlayerMaterialPath} / {EnemyMaterialPath} (and fallback {FallbackUnitMaterialPath})");
            var ringFillShader = LoadOrFlag<Shader>(RingFillShaderPath, missing);
            var gradeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GradeProfilePath);
            if (gradeProfile == null)
                Debug.LogWarning($"[Combat3D] Grade volume profile missing at {GradeProfilePath} — scene will render ungraded.");

            var idleClip = FindClip(IdleGlbPath, missing, "idle");
            var walkClip = FindClip(WalkGlbPath, missing, "walk");
            var dieClip = FindClip(DieGlbPath, missing, "dead", "die");

            if (missing.Count > 0)
            {
                Debug.LogError(
                    "[Combat3D] Aborting — required Phase-0 spike assets are missing:\n - " +
                    string.Join("\n - ", missing));
                return null;
            }

            // --- Generated assets under _Project (spike stays throwaway). ---
            EnsureFolder(GeneratedFolder);
            // Placeholder SFX set + ambience bed (no real combat audio exists in the project yet).
            var (audioSet, ambienceLoop) = Combat3DPlaceholderAudioBuilder.EnsureAudioSet();
            return new SceneAssets
            {
                IdleModel = idleModel,
                PlayerMat = playerMat,
                EnemyMat = enemyMat,
                GradeProfile = gradeProfile,
                RingBlue = LoadOrCreateRingFillMaterial(
                    PlayerRingPath, ringFillShader,
                    fill: new Color(0.20f, 0.28f, 0.42f),   // spike RingBlue
                    rim: new Color(0.28f, 0.40f, 0.60f),
                    empty: new Color(0.09f, 0.10f, 0.13f)),
                RingRed = LoadOrCreateRingFillMaterial(
                    EnemyRingPath, ringFillShader,
                    fill: new Color(0.40f, 0.18f, 0.16f),   // spike RingRed
                    rim: new Color(0.56f, 0.25f, 0.21f),
                    empty: new Color(0.12f, 0.09f, 0.09f)),
                Controller = BuildAnimatorController(
                    idleClip, walkClip, dieClip, IdleClipPath, WalkClipPath, DieClipPath, ControllerPath),
                Archetypes = BuildRosterArchetypes(),
                Config = BuildDemoArenaConfig(),
                RiflePrefab = RiflePropBuilder.EnsurePrefab(),
                AudioSet = audioSet,
                AmbienceLoop = ambienceLoop
            };
        }

        [MenuItem("DeadManZone/Combat3D/Build Combat3D Demo Scene")]
        public static void BuildDemoScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Combat3D] Exit Play mode before building the demo scene.");
                return;
            }

            var assets = PrepareSceneAssets();
            if (assets == null)
                return;

            // --- Scene ---
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[Combat3D] Scene build cancelled (unsaved scene).");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironmentLighting();
            var camera = CreateCamera(includeAudioListener: true);
            CreateKeyLight();
            CreateGlobalVolume(assets.GradeProfile);
            CombatEnvironmentBuilder.Build();
            CreateArenaRig(camera, assets.Config, assets.Controller, assets.IdleModel, assets.PlayerMat,
                assets.EnemyMat, assets.RingBlue, assets.RingRed, assets.RiflePrefab, assets.Archetypes,
                assets.AudioSet, assets.AmbienceLoop);

            EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Combat3D] Demo scene saved to {ScenePath}. Open it and press Play — " +
                      "the 3v3 fight auto-starts through the Core sim.");
        }

        private static T LoadOrFlag<T>(string path, List<string> missing) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                missing.Add(path);
            return asset;
        }

        private static AnimationClip FindClip(string glbPath, List<string> missing, params string[] keywords)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(glbPath)
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.Contains("__preview"))
                .ToList();

            foreach (string keyword in keywords)
            {
                var match = clips.FirstOrDefault(c =>
                    c.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null)
                    return match;
            }

            // Single-clip GLBs: take the only clip rather than failing on a rename.
            if (clips.Count == 1)
                return clips[0];

            missing.Add($"{glbPath} (no AnimationClip matching '{string.Join("/", keywords)}'; " +
                        $"found: {string.Join(", ", clips.Select(c => c.name))})");
            return null;
        }

        /// <summary>One archetype entry per roster unit whose GLBs imported cleanly:
        /// generated looped .anim copies + AnimatorController (same logic as the rifleman)
        /// living next to the unit's GLBs under Combat3D/Models/&lt;unit&gt;/. Vehicle units
        /// append after: static model.glb only, no controller.</summary>
        private static List<ArchetypeSpec> BuildRosterArchetypes()
        {
            var archetypes = new List<ArchetypeSpec>();
            var covered = new HashSet<string>();
            foreach (var (unitFolder, pieceId) in RosterUnits)
            {
                string folder = RosterModelFolder(unitFolder);
                var unitMissing = new List<string>();
                var model = LoadOrFlag<GameObject>(folder + "/idle.glb", unitMissing);
                var idle = FindClip(folder + "/idle.glb", unitMissing, "idle");
                var walk = FindClip(folder + "/walk.glb", unitMissing, "walk");
                var die = FindClip(folder + "/die.glb", unitMissing, "dead", "die");

                if (unitMissing.Count > 0)
                {
                    Debug.LogWarning(
                        $"[Combat3D] Roster unit '{unitFolder}' skipped (falls back to rifleman visuals):\n - " +
                        string.Join("\n - ", unitMissing));
                    continue;
                }

                var controller = BuildAnimatorController(
                    idle, walk, die,
                    $"{folder}/{unitFolder}_Idle.anim",
                    $"{folder}/{unitFolder}_Walk.anim",
                    $"{folder}/{unitFolder}_Die.anim",
                    $"{folder}/{unitFolder}Combat3D.controller");
                archetypes.Add(new ArchetypeSpec
                {
                    PieceId = pieceId,
                    Model = model,
                    Controller = controller,
                });
                covered.Add(pieceId);
            }

            archetypes.AddRange(BuildHumanoidReuseArchetypes(covered));

            foreach (var (unitFolder, pieceId, height, yaw) in VehicleUnits)
            {
                string path = RosterModelFolder(unitFolder) + "/model.glb";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    Debug.LogWarning(
                        $"[Combat3D] Vehicle unit '{unitFolder}' skipped — no {path} yet " +
                        "(run tools/meshy/generate_unit.py <unit> --vehicle); falls back to rifleman visuals.");
                    continue;
                }

                archetypes.Add(new ArchetypeSpec
                {
                    PieceId = pieceId,
                    Model = model,
                    IsVehicle = true,
                    VehicleHeight = height,
                    VehicleYawOffsetDegrees = yaw,
                });
            }

            archetypes.AddRange(BuildPrimitiveFallbackArchetypes());

            return archetypes;
        }

        /// <summary>Every PieceDefinitionSO under the Pieces folder that is combat-eligible
        /// (category Unit) infantry and not already covered by <see cref="RosterUnits"/>:
        /// grouped by its combatRole's reuse folder, one controller/clip set built per folder
        /// (see the comment on <see cref="HumanoidReuseFolderByCombatRole"/> for why), then one
        /// archetype per piece sharing that folder's model+controller.</summary>
        private static List<ArchetypeSpec> BuildHumanoidReuseArchetypes(HashSet<string> alreadyCovered)
        {
            var byFolder = new Dictionary<string, List<string>>();
            var guids = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PiecesFolder });
            foreach (var guid in guids)
            {
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (piece == null || alreadyCovered.Contains(piece.id))
                    continue;
                if (piece.category != PieceCategory.Unit || piece.primary != "infantry")
                    continue;

                string folder = HumanoidReuseFolderByCombatRole.TryGetValue(piece.combatRole ?? "", out var mapped)
                    ? mapped
                    : HumanoidReuseDefaultFolder;
                if (!byFolder.TryGetValue(folder, out var pieceIds))
                    byFolder[folder] = pieceIds = new List<string>();
                pieceIds.Add(piece.id);
            }

            var archetypes = new List<ArchetypeSpec>();
            foreach (var (unitFolder, pieceIds) in byFolder)
            {
                string folder = RosterModelFolder(unitFolder);
                var unitMissing = new List<string>();
                var model = LoadOrFlag<GameObject>(folder + "/idle.glb", unitMissing);
                var idle = FindClip(folder + "/idle.glb", unitMissing, "idle");
                var walk = FindClip(folder + "/walk.glb", unitMissing, "walk");
                var die = FindClip(folder + "/die.glb", unitMissing, "dead", "die");

                if (unitMissing.Count > 0)
                {
                    Debug.LogWarning(
                        $"[Combat3D] Humanoid reuse folder '{unitFolder}' unavailable — " +
                        $"{pieceIds.Count} piece(s) fall back to rifleman visuals:\n - " +
                        string.Join("\n - ", unitMissing));
                    continue;
                }

                // Distinct asset paths from RosterUnits' own controller for this folder
                // (suffixed _Reuse) — this is a SEPARATE build shared by every mapped piece,
                // not a rebuild of the folder's primary archetype.
                var controller = BuildAnimatorController(
                    idle, walk, die,
                    $"{folder}/{unitFolder}_Reuse_Idle.anim",
                    $"{folder}/{unitFolder}_Reuse_Walk.anim",
                    $"{folder}/{unitFolder}_Reuse_Die.anim",
                    $"{folder}/{unitFolder}_ReuseCombat3D.controller");

                foreach (var pieceId in pieceIds)
                {
                    archetypes.Add(new ArchetypeSpec
                    {
                        PieceId = pieceId,
                        Model = model,
                        Controller = controller,
                    });
                }
            }

            return archetypes;
        }

        /// <summary>Outlined grey-box placeholder for every combat-eligible vehicle/structure
        /// with no model yet (<see cref="PrimitiveFallbackUnits"/>) — same ArchetypeSpec shape
        /// as <see cref="VehicleUnits"/> (IsVehicle=true, no controller), so it renders through
        /// the existing CombatUnitVisual3DVehicle path and inherits its side material (cel/ink
        /// shading, hit-flash, dissolve) automatically; nothing extra to wire there.</summary>
        private static List<ArchetypeSpec> BuildPrimitiveFallbackArchetypes()
        {
            var archetypes = new List<ArchetypeSpec>();
            foreach (var (pieceId, cellsX, cellsY, kind, height) in PrimitiveFallbackUnits)
            {
                var model = EnsurePrimitiveModel(pieceId, cellsX, cellsY, kind);
                if (model == null)
                    continue;

                archetypes.Add(new ArchetypeSpec
                {
                    PieceId = pieceId,
                    Model = model,
                    IsVehicle = true,
                    VehicleHeight = height,
                    VehicleYawOffsetDegrees = 0f,
                });
            }

            return archetypes;
        }

        private const string PiecesFolder = "Assets/_Project/Data/Resources/DeadManZone/Pieces";
        private const string PrimitivesFolder = GeneratedFolder + "/Models/Primitives";

        /// <summary>Builds (once) or loads a generated grey-box prefab sized to the piece's
        /// footprint (shapeCells count) with a per-kind greeble so it reads as an intentional
        /// placeholder, not an empty crate. CombatUnitVisual3DVehicle.Build() overrides every
        /// renderer's material with the side's tuned toon-ink material, so no material work is
        /// needed here — Unity's default primitive material is only ever visible for one frame
        /// before Build() runs, if that.</summary>
        private static GameObject EnsurePrimitiveModel(string pieceId, int cellsX, int cellsY, PrimitiveKind kind)
        {
            string path = $"{PrimitivesFolder}/{pieceId}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            EnsureFolder(PrimitivesFolder);

            float width = Mathf.Max(1, cellsX) * 1.5f;
            float depth = Mathf.Max(1, cellsY) * 1.5f;
            float bodyHeight = kind == PrimitiveKind.Wall ? width * 0.45f : width * 0.55f;

            var root = new GameObject(pieceId + "_Primitive");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.localScale = new Vector3(width, bodyHeight, depth);
            body.transform.localPosition = new Vector3(0f, bodyHeight * 0.5f, 0f);

            if (kind == PrimitiveKind.Tank)
            {
                float turretSize = width * 0.45f;
                var turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
                turret.name = "Turret";
                turret.transform.SetParent(root.transform, false);
                UnityEngine.Object.DestroyImmediate(turret.GetComponent<Collider>());
                turret.transform.localScale = new Vector3(turretSize, turretSize * 0.6f, turretSize);
                turret.transform.localPosition = new Vector3(0f, bodyHeight + turretSize * 0.3f, 0f);

                float barrelLength = width * 0.6f;
                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = "Barrel";
                barrel.transform.SetParent(root.transform, false);
                UnityEngine.Object.DestroyImmediate(barrel.GetComponent<Collider>());
                barrel.transform.localScale = new Vector3(turretSize * 0.16f, barrelLength * 0.5f, turretSize * 0.16f);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // cylinder axis (+Y) -> +Z
                barrel.transform.localPosition =
                    new Vector3(0f, bodyHeight + turretSize * 0.3f, depth * 0.5f + barrelLength * 0.35f);
            }
            else if (kind == PrimitiveKind.Structure)
            {
                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.name = "RoofDetail";
                roof.transform.SetParent(root.transform, false);
                UnityEngine.Object.DestroyImmediate(roof.GetComponent<Collider>());
                roof.transform.localScale = new Vector3(width * 0.4f, bodyHeight * 0.3f, depth * 0.4f);
                roof.transform.localPosition = new Vector3(0f, bodyHeight + bodyHeight * 0.15f, 0f);
            }
            // Wall: bare slab, no added greeble.

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);

            // Same AssetDatabase lesson as DemoContentGenerator.LoadOrCreate: force the import
            // to complete and hand back the canonical imported instance, not the pre-import one.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static AnimatorController BuildAnimatorController(
            AnimationClip idleSource, AnimationClip walkSource, AnimationClip dieSource,
            string idleClipPath, string walkClipPath, string dieClipPath, string controllerPath)
        {
            // Writable copies so loop flags can be set (imported GLB sub-assets are read-only).
            var idle = SaveClipCopy(idleSource, idleClipPath, loop: true);
            var walk = SaveClipCopy(walkSource, walkClipPath, loop: true);
            var die = SaveClipCopy(dieSource, dieClipPath, loop: false);

            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = stateMachine.AddState("Idle");
            idleState.motion = idle;
            var walkState = stateMachine.AddState("Walk");
            walkState.motion = walk;
            var dieState = stateMachine.AddState("Die");
            dieState.motion = die;
            stateMachine.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

            var anyToDie = stateMachine.AddAnyStateTransition(dieState);
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0.08f;
            anyToDie.canTransitionToSelf = false;
            anyToDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip SaveClipCopy(AnimationClip source, string assetPath, bool loop)
        {
            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(copy, settings);

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(copy, assetPath);
            return copy;
        }

        private static CombatArenaConfigSO BuildDemoArenaConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.visualMode = CombatArenaVisualMode.ToonInk3D;
            config.cellWidth = 1.8f;
            config.cellDepth = 1.8f;
            // Reuse the v4 pacing: free-chase march + calibrated walk speed, untouched.
            config.useTopTroopsFreeChaseMovement = true;
            config.useTopTroopsProceduralBattlefield = false;
            config.showCheckerboardGrid = false;
            config.enableArenaFog = false; // scene RenderSettings own the fog
            config.useSyntyTerrain = false;
            config.spawnPerimeterProps = false;

            EditorUtility.SetDirty(config);
            return config;
        }

        /// <summary>Copy a spike unit material to the generated folder (once) and apply the
        /// readability tune: lifted shadow band + softer rim ink for gameplay camera distance.</summary>
        private static Material LoadOrCreateTunedUnitMaterial(string sourcePath, string tunedPath)
        {
            var tuned = AssetDatabase.LoadAssetAtPath<Material>(tunedPath);
            if (tuned == null)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(sourcePath) == null)
                    return null;
                if (!AssetDatabase.CopyAsset(sourcePath, tunedPath))
                    return null;
                tuned = AssetDatabase.LoadAssetAtPath<Material>(tunedPath);
            }

            tuned.SetColor("_ShadowColor", new Color(0.45f, 0.44f, 0.50f));
            tuned.SetFloat("_ShadowThreshold", -0.15f);
            tuned.SetFloat("_InkStrength", 0.28f);
            tuned.SetFloat("_MidStrength", 0.45f);
            // Inverted-hull outline fragments into scribble on Meshy skinned normals; the
            // combat renderer's fullscreen depth/normal edge detect draws the clean
            // silhouette instead, so units run with the hull pass off.
            tuned.SetFloat("_OutlineWidth", 0f);
            EditorUtility.SetDirty(tuned);
            return tuned;
        }

        /// <summary>Generated DMZ/CombatRingFill material for one side. Colors reset on
        /// every build (same pattern as the tuned unit materials) so palette tweaks here
        /// propagate through a menu rebuild.</summary>
        private static Material LoadOrCreateRingFillMaterial(
            string path, Shader shader, Color fill, Color rim, Color empty)
        {
            if (shader == null)
                return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_FillColor", fill);
            material.SetColor("_RimColor", rim);
            material.SetColor("_EmptyColor", empty);
            material.SetFloat("_Fill", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // --- Scene construction (values lifted from Assets/_Phase0Spike/Scenes/Phase0_Spike.unity) ---

        /// <summary>Per-theme RenderSettings (M4). Null = Trenchline, whose values were
        /// lifted from the spike then brightened so ToonInk's SampleSH term keeps
        /// shadow-side unit detail readable at gameplay camera distance. Ambient mode and
        /// fog mode are law across themes; only the colors/density are theme knobs.</summary>
        internal static void ApplyEnvironmentLighting(ArenaThemeProfile profile = null)
        {
            profile ??= CombatArenaThemeProfiles.Trenchline;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.AmbientSky;
            RenderSettings.ambientEquatorColor = profile.AmbientEquator;
            RenderSettings.ambientGroundColor = profile.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = profile.FogColor;
            // Trenchline 0.022: spike used 0.028 on a smaller field; 0.022 sells backdrop
            // depth without eating units (~20% fog at the far lane).
            RenderSettings.fogDensity = profile.FogDensity;
        }

        /// <param name="includeAudioListener">Demo scene: true (nothing else provides one).
        /// Run-flow arena scene: false — CombatArenaUiController.EnterArenaMode moves the
        /// Run scene's listener onto the arena camera; a serialized one would duplicate.</param>
        internal static Camera CreateCamera(bool includeAudioListener)
        {
            var go = new GameObject("ArenaCamera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            if (includeAudioListener)
                go.AddComponent<AudioListener>();
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Slightly above RenderSettings.fogColor: the fog line at the terrain crest is
            // key-lit ground mixed with fog, so a bg at raw fog color reads as a dark seam.
            // Solid grimdark sky per arena spec; the backdrop ring anchors the horizon.
            camera.backgroundColor = new Color(0.17f, 0.19f, 0.24f);
            // Trenchworks framing (candidates in docs/framing/, 2026-07-11): pulled back
            // from (0, 8.5, -12, pitch 30, fov 40) so the far parapets/craters/wire line
            // sit inside the frame; the shallower pitch lifts the backdrop instead of
            // adding dead foreground. Units stay ~80% of the old on-screen height —
            // still well inside the readable-ink range the previous close-up protected.
            go.transform.position = new Vector3(0f, 10f, -14f);
            go.transform.rotation = Quaternion.Euler(29f, 0f, 0f);

            // Combat-only rendering: the interior-ink + SSAO features live on
            // DeadManZone_CombatRenderer, not the shared forward renderer, so
            // non-combat scenes stay clean. Opt this camera into it by index.
            int rendererIndex = FindCombatRendererIndex();
            if (rendererIndex >= 0)
                camera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);
            return camera;
        }

        /// <summary>Index of DeadManZone_CombatRenderer in the active URP asset's renderer
        /// list (serialized onto the camera, so the scene keeps working at runtime).</summary>
        private static int FindCombatRendererIndex()
        {
            var pipeline = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline)
                as UniversalRenderPipelineAsset;
            if (pipeline == null)
            {
                Debug.LogWarning("[Combat3D] Active render pipeline is not URP — camera keeps the default renderer (no interior ink/SSAO).");
                return -1;
            }

            var rendererList = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            for (int i = 0; i < rendererList.arraySize; i++)
            {
                var data = rendererList.GetArrayElementAtIndex(i).objectReferenceValue;
                if (data != null && AssetDatabase.GetAssetPath(data) == CombatRendererPath)
                    return i;
            }

            Debug.LogWarning($"[Combat3D] {CombatRendererPath} is not in the renderer list of " +
                             $"{AssetDatabase.GetAssetPath(pipeline)} — camera keeps the default renderer (no interior ink/SSAO).");
            return -1;
        }

        internal static void CreateKeyLight()
        {
            var go = new GameObject("Key Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.92f, 0.80f); // warm key (spike value)
            light.intensity = 1.7f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.9f;
            go.transform.rotation = Quaternion.Euler(40f, -52f, 0f); // spike value

            // Cool shadowless fill so the ToonInk shadow band never collapses to black.
            var fillGo = new GameObject("Fill Light");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.75f, 0.80f, 0.90f);
            fill.intensity = 0.45f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(25f, 160f, 0f);
        }

        internal static void CreateGlobalVolume(VolumeProfile profile)
        {
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile; // P0_Grade (null tolerated; warned above)
        }

        private static void CreateArenaRig(
            Camera camera,
            CombatArenaConfigSO config,
            AnimatorController controller,
            GameObject unitModel,
            Material playerMat,
            Material enemyMat,
            Material ringBlue,
            Material ringRed,
            GameObject riflePrefab,
            List<ArchetypeSpec> archetypes,
            CombatArenaAudioSetSO audioSet,
            AudioClip ambienceLoop)
        {
            var rig = new GameObject("Combat3DArena");

            var unitsRoot = new GameObject("UnitsRoot");
            unitsRoot.transform.SetParent(rig.transform, false);
            var buildingsRoot = new GameObject("BuildingsRoot");
            buildingsRoot.transform.SetParent(rig.transform, false);

            var bootstrap = rig.AddComponent<CombatArenaBootstrap>();
            SetSerialized(bootstrap, so =>
            {
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
                so.FindProperty("unitsRoot").objectReferenceValue = unitsRoot.transform;
                so.FindProperty("buildingsRoot").objectReferenceValue = buildingsRoot.transform;
                so.FindProperty("config").objectReferenceValue = config;
            });

            var director = rig.AddComponent<CombatDirector>();

            // One-shot SFX presenter (CombatArenaPresenter finds it on the rig and drives
            // it from replayed muzzle/impact/death events). Demo-only placeholder set —
            // the presenter's Resources fallback is the shared 2D asset, left untouched.
            var arenaAudio = rig.AddComponent<CombatArenaAudioPresenter>();
            SetSerialized(arenaAudio, so =>
            {
                so.FindProperty("audioSet").objectReferenceValue = audioSet;
            });

            // Ambient bed: plain looping 2D source, kept well under the SFX (bible §3 —
            // the ambience is a floor, not a feature).
            if (ambienceLoop != null)
            {
                var ambience = new GameObject("AmbienceBed");
                ambience.transform.SetParent(rig.transform, false);
                var ambienceSource = ambience.AddComponent<AudioSource>();
                ambienceSource.clip = ambienceLoop;
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = true;
                ambienceSource.volume = 0.16f;
                ambienceSource.spatialBlend = 0f;
            }

            var presenter = rig.AddComponent<CombatArenaPresenter>();
            SetSerialized(presenter, so =>
            {
                so.FindProperty("combatDirector").objectReferenceValue = director;
            });

            var loader = rig.AddComponent<CombatArenaSceneLoader>();

            var installer = rig.AddComponent<CombatUnitVisual3DInstaller>();
            SetSerialized(installer, so =>
            {
                so.FindProperty("unitModel").objectReferenceValue = unitModel;
                so.FindProperty("animatorController").objectReferenceValue = controller;
                so.FindProperty("playerUnitMaterial").objectReferenceValue = playerMat;
                so.FindProperty("enemyUnitMaterial").objectReferenceValue = enemyMat;
                so.FindProperty("playerRingMaterial").objectReferenceValue = ringBlue;
                so.FindProperty("enemyRingMaterial").objectReferenceValue = ringRed;
                so.FindProperty("riflePrefab").objectReferenceValue = riflePrefab;

                WriteArchetypes(so, archetypes);
            });

            // Army health HUD: two opposing top-of-screen bars fed by the replay tracker.
            var armyHud = rig.AddComponent<CombatArmyHealthHud>();
            SetSerialized(armyHud, so =>
            {
                so.FindProperty("director").objectReferenceValue = director;
            });

            var driver = rig.AddComponent<Combat3DDemoDriver>();
            SetSerialized(driver, so =>
            {
                so.FindProperty("director").objectReferenceValue = director;
                so.FindProperty("presenter").objectReferenceValue = presenter;
                so.FindProperty("arenaLoader").objectReferenceValue = loader;
                so.FindProperty("armyHud").objectReferenceValue = armyHud;
            });

            // Feel pass: punch-in camera beats + pooled muzzle-flash VFX (arena spec §1/§6).
            var punchIn = rig.AddComponent<CombatArenaPunchInCamera>();
            SetSerialized(punchIn, so =>
            {
                so.FindProperty("director").objectReferenceValue = director;
                so.FindProperty("presenter").objectReferenceValue = presenter;
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
            });

            var vfx = rig.AddComponent<Combat3DVfxPresenter>();
            SetSerialized(vfx, so =>
            {
                so.FindProperty("arenaCamera").objectReferenceValue = camera;
            });
        }

        /// <summary>Serialize the installer's archetypes array (shared by the demo and the
        /// run-flow scene builders — keep the field writes in one place).</summary>
        internal static void WriteArchetypes(SerializedObject so, List<ArchetypeSpec> archetypes)
        {
            var archetypeArray = so.FindProperty("archetypes");
            archetypeArray.arraySize = archetypes.Count;
            for (int i = 0; i < archetypes.Count; i++)
            {
                var element = archetypeArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("pieceId").stringValue = archetypes[i].PieceId;
                element.FindPropertyRelative("model").objectReferenceValue = archetypes[i].Model;
                element.FindPropertyRelative("controller").objectReferenceValue = archetypes[i].Controller;
                element.FindPropertyRelative("isVehicle").boolValue = archetypes[i].IsVehicle;
                element.FindPropertyRelative("vehicleHeight").floatValue = archetypes[i].VehicleHeight;
                element.FindPropertyRelative("vehicleYawOffsetDegrees").floatValue =
                    archetypes[i].VehicleYawOffsetDegrees;
            }
        }

        internal static void SetSerialized(Component component, Action<SerializedObject> apply)
        {
            var serialized = new SerializedObject(component);
            apply(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
// EOF
     