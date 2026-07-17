using System;
using DeadManZone.Core;
using DeadManZone.Core.Combat;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Scene-level opt-in for the 3D toon-ink unit backend: while enabled, installs
    /// <see cref="CombatUnitActor.VisualFactory"/> so pooled actors build a
    /// <see cref="CombatUnitVisual3D"/> instead of the default 2D sprite visual.
    /// Side channel per owner decision: blue ring = player side, red ring = enemy side
    /// (outline side tint stays off — side never touches the model).
    /// </summary>
    public sealed class CombatUnitVisual3DInstaller : MonoBehaviour
    {
        /// <summary>Per-archetype model/controller override, matched on the piece's content id
        /// (<see cref="PieceDefinitionSO.id"/> — the same id the Core sim uses). Unknown or
        /// incomplete entries fall back to the default (rifleman) model/controller.</summary>
        [Serializable]
        private struct ArchetypeVisual
        {
            public string pieceId;
            public GameObject model;
            public RuntimeAnimatorController controller;
            [Tooltip("Non-humanoid (tank/transport/emplacement): static mesh, code-driven " +
                     "motion via CombatUnitVisual3DVehicle — no controller required.")]
            public bool isVehicle;
            [Tooltip("Vehicle silhouette height in meters (vehicles are not 1.7 m infantry). " +
                     "0 = fall back to the installer's unitHeight.")]
            public float vehicleHeight;
            [Tooltip("Per-model facing correction (degrees) — Meshy vehicle gens from " +
                     "side-view refs come out with arbitrary authored forward axes.")]
            public float vehicleYawOffsetDegrees;
        }

        [SerializeField] private GameObject unitModel;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private ArchetypeVisual[] archetypes = Array.Empty<ArchetypeVisual>();
        [SerializeField] private Material playerUnitMaterial;
        [SerializeField] private Material enemyUnitMaterial;
        [SerializeField] private Material playerRingMaterial;
        [SerializeField] private Material enemyRingMaterial;
        [Tooltip("Shared rifle prop parented to each unit's right hand (optional).")]
        [SerializeField] private GameObject riflePrefab;
        [SerializeField] private float unitHeight = 1.7f;
        [Tooltip("Extra yaw (degrees) if the model's authored forward is not +Z.")]
        [SerializeField] private float modelYawOffsetDegrees;

        private Func<CombatUnitActor, PieceDefinitionSO, CombatSide, Camera, ICombatUnitVisual>
            _factory;

        private bool _loggedMissingAssets;

        private void OnEnable()
        {
            _factory ??= CreateVisual;
            CombatUnitActor.VisualFactory = _factory;
        }

        private void OnDisable()
        {
            if (ReferenceEquals(CombatUnitActor.VisualFactory, _factory))
                CombatUnitActor.VisualFactory = null;
        }

        private ICombatUnitVisual CreateVisual(
            CombatUnitActor actor,
            PieceDefinitionSO piece,
            CombatSide side,
            Camera arenaCamera)
        {
            var model = unitModel;
            var controller = animatorController;
            var factionTint = ResolveFactionTint(piece);
            if (piece != null && archetypes != null)
            {
                for (int i = 0; i < archetypes.Length; i++)
                {
                    if (archetypes[i].pieceId != piece.id || archetypes[i].model == null)
                        continue;

                    // Vehicles are static meshes with code-driven motion — no controller.
                    if (archetypes[i].isVehicle)
                    {
                        var vehicleVisual = actor.gameObject.AddComponent<CombatUnitVisual3DVehicle>();
                        vehicleVisual.Build(
                            archetypes[i].model,
                            side == CombatSide.Player ? playerUnitMaterial : enemyUnitMaterial,
                            side == CombatSide.Player ? playerRingMaterial : enemyRingMaterial,
                            archetypes[i].vehicleHeight > 0f ? archetypes[i].vehicleHeight : unitHeight,
                            archetypes[i].vehicleYawOffsetDegrees,
                            factionTint);
                        return vehicleVisual;
                    }

                    if (archetypes[i].controller != null)
                    {
                        model = archetypes[i].model;
                        controller = archetypes[i].controller;
                    }

                    break;
                }
            }

            if (model == null || controller == null)
            {
                if (!_loggedMissingAssets)
                {
                    _loggedMissingAssets = true;
                    Debug.LogError(
                        "[Combat3D] CombatUnitVisual3DInstaller is missing its unit model or " +
                        "animator controller — rebuild the scene via DeadManZone → Combat3D → " +
                        "Build Combat3D Demo Scene. Falling back to 2D unit visuals.",
                        this);
                }

                return null; // actor falls back to the 2D sprite backend
            }

            var visual = actor.gameObject.AddComponent<CombatUnitVisual3D>();
            visual.Build(
                model,
                controller,
                side == CombatSide.Player ? playerUnitMaterial : enemyUnitMaterial,
                side == CombatSide.Player ? playerRingMaterial : enemyRingMaterial,
                riflePrefab,
                unitHeight,
                modelYawOffsetDegrees,
                factionTint);
            return visual;
        }

        /// <summary>Faction accent color for the ring rim (Wave 3 placeholder-art pass):
        /// prefers the faction's authored tokenBackgroundColor, falls back to the same
        /// palette PieceArtResolver uses for board chips so the two stay visually related.
        /// Deliberately independent of UiThemeProvider (unlike PieceArtResolver) — the
        /// Combat3D scenes can build a unit before any shop UI has initialized a theme.</summary>
        private static Color ResolveFactionTint(PieceDefinitionSO piece)
        {
            if (piece == null || string.IsNullOrWhiteSpace(piece.factionId) || piece.factionId == "neutral")
                return Color.clear;

            var faction = ContentDatabase.Load()?.GetFaction(piece.factionId);
            if (faction != null && faction.tokenBackgroundColor.a > 0.01f)
                return faction.tokenBackgroundColor;

            return piece.factionId switch
            {
                FactionIds.IronmarchUnion => new Color(0.22f, 0.28f, 0.38f, 0.45f),
                FactionIds.DustScourge => new Color(0.42f, 0.34f, 0.24f, 0.45f),
                FactionIds.CartelOfEchoes => new Color(0.32f, 0.26f, 0.42f, 0.45f),
                FactionIds.CrimsonAssembly => new Color(0.45f, 0.20f, 0.18f, 0.45f),
                FactionIds.AshenCovenant => new Color(0.28f, 0.28f, 0.30f, 0.45f),
                FactionIds.OathbornAccord => new Color(0.42f, 0.36f, 0.18f, 0.45f),
                FactionIds.ParadoxEngine => new Color(0.20f, 0.34f, 0.40f, 0.45f),
                FactionIds.BlightbornPact => new Color(0.24f, 0.38f, 0.20f, 0.45f),
                _ => Color.clear
            };
        }
    }
}
