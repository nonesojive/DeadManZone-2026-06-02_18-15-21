using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Owns grim atmosphere lifecycle: apply on frame, clear on rebuild or destroy.</summary>
    public sealed class CombatArenaAtmosphereController : MonoBehaviour
    {
        private const string ControllerName = "CombatArenaAtmosphere";

        public static CombatArenaAtmosphereController Ensure(Transform arenaRoot)
        {
            if (arenaRoot == null)
                return null;

            var existing = arenaRoot.Find(ControllerName);
            if (existing != null &&
                existing.TryGetComponent<CombatArenaAtmosphereController>(out var controller))
                return controller;

            var go = new GameObject(ControllerName);
            go.transform.SetParent(arenaRoot, false);
            return go.AddComponent<CombatArenaAtmosphereController>();
        }

        public void Apply(
            CombatArenaAtmosphereProfileSO profile,
            CombatArenaConfigSO config,
            Camera arenaCamera)
        {
            ClearOwnedSceneObjects();
            CombatArenaAtmosphereApplicator.Apply(profile, config, transform, arenaCamera);
        }

        public void ClearOwnedSceneObjects()
        {
            CombatArenaAtmosphereApplicator.ClearAtmosphere(transform);
        }

        private void OnDestroy() => ClearOwnedSceneObjects();
    }
}
