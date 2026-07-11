using System;
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
        }

        [SerializeField] private GameObject unitModel;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private ArchetypeVisual[] archetypes = Array.Empty<ArchetypeVisual>();
        [SerializeField] private Material playerUnitMaterial;
        [SerializeField] private Material enemyUnitMaterial;
        [SerializeField] private Material playerRingMaterial;
        [SerializeField] private Material enemyRingMaterial;
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
            if (piece != null && archetypes != null)
            {
                for (int i = 0; i < archetypes.Length; i++)
                {
                    if (archetypes[i].pieceId == piece.id &&
                        archetypes[i].model != null &&
                        archetypes[i].controller != null)
                    {
                        model = archetypes[i].model;
                        controller = archetypes[i].controller;
                        break;
                    }
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
                unitHeight,
                modelYawOffsetDegrees);
            return visual;
        }
    }
}
