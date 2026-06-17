using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Facade for the modular backdrop assembler (Approach B).</summary>
    public sealed class CombatArenaBackdrop : MonoBehaviour
    {
        private const string RootName = "CombatArenaBackdrop";

        public static CombatArenaBackdrop Build(
            Transform arenaRoot,
            BattlefieldLayout layout,
            CombatArenaConfigSO config,
            CombatArenaAtmosphereProfileSO profile)
        {
            if (arenaRoot == null || layout == null || config == null || profile == null || !profile.enableBackdrop)
                return null;

            var existing = arenaRoot.Find(RootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(arenaRoot, false);
            var backdrop = rootGo.AddComponent<CombatArenaBackdrop>();
            CombatArenaBackdropAssembler.Populate(backdrop.transform, layout, config, profile);
            return backdrop;
        }
    }
}
