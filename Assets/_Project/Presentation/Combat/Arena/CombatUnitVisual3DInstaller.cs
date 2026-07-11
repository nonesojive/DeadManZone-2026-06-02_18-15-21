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
        [SerializeField] private GameObject unitModel;
        [SerializeField] private RuntimeAnimatorController animatorController;
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
            if (unitModel == null || animatorController == null)
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
                unitModel,
                animatorController,
                side == CombatSide.Player ? playerUnitMaterial : enemyUnitMaterial,
                side == CombatSide.Player ? playerRingMaterial : enemyRingMaterial,
                unitHeight,
                modelYawOffsetDegrees);
            return visual;
        }
    }
}
