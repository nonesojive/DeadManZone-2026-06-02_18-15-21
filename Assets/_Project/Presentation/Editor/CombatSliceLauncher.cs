#if UNITY_EDITOR
using DeadManZone.Data.Editor;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class CombatSliceLauncher
    {
        [MenuItem("DeadManZone/Combat Arena/Launch Iron Vanguard Slice (Bootstrap All)")]
        public static void BootstrapAll()
        {
            ApocalypseCombatHudSetup.ImportApocalypseCombatHud();
            CombatArenaVfxSetBootstrap.CreateOrRefresh();
            CombatArenaAnimationSetBootstrap.CreateOrRefresh();
            CombatArenaCombatAnimatorBootstrap.RebuildCombatInfantryAnimator();
            CombatArenaCombatAnimatorBootstrap.AssignCombatAnimatorToArenaUnits();
            CombatSliceEnvironmentBootstrap.ApplySliceEnvironment();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Iron Vanguard slice assets bootstrapped.\n" +
                "Next: open Run scene, begin combat with slice layout, capture screenshots.");
        }
    }
}
#endif
