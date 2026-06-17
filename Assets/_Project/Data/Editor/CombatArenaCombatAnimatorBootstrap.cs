#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// Duplicates Synty locomotion controller and adds a Combat layer with shoot/grenade/death triggers.
    /// </summary>
    public static class CombatArenaCombatAnimatorBootstrap
    {
        private const string OutputPath =
            "Assets/_Project/Art/Synty/Animation/AC_CombatArena_Infantry.controller";
        private const string LocomotionControllerPath =
            "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/AC_Polygon_Masculine.controller";
        private const string AnimationSetPath =
            "Assets/_Project/Data/Resources/DeadManZone/CombatArenaAnimationSet.asset";

        [MenuItem("DeadManZone/Combat Arena/Create Combat Infantry Animator")]
        public static void CreateCombatInfantryAnimator()
        {
            RebuildCombatInfantryAnimator();
        }

        [MenuItem("DeadManZone/Combat Arena/Rebuild Combat Infantry Animator")]
        public static void RebuildCombatInfantryAnimator()
        {
            var animationSet = AssetDatabase.LoadAssetAtPath<CombatArenaAnimationSetSO>(AnimationSetPath);
            if (animationSet == null || animationSet.rifleShoot == null)
            {
                Debug.LogError("Run DeadManZone → Combat Arena → Create Or Refresh Animation Set first.");
                return;
            }

            EnsureFolder("Assets/_Project/Art/Synty/Animation");

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputPath) == null)
            {
                if (!AssetDatabase.CopyAsset(LocomotionControllerPath, OutputPath))
                {
                    Debug.LogError($"Failed to copy locomotion controller to {OutputPath}");
                    return;
                }
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputPath);
            if (controller == null)
            {
                Debug.LogError($"Missing controller at {OutputPath}");
                return;
            }

            AddTriggerIfMissing(controller, "Shoot");
            AddTriggerIfMissing(controller, "GrenadeThrow");
            AddTriggerIfMissing(controller, "Death");

            RemoveLayerNamed(controller, "Combat");
            controller.AddLayer("Combat");
            var combatLayer = controller.layers[controller.layers.Length - 1];
            combatLayer.defaultWeight = 1f;
            combatLayer.avatarMask = null;
            combatLayer.blendingMode = AnimatorLayerBlendingMode.Override;

            var sm = combatLayer.stateMachine;
            var empty = sm.AddState("Empty", new Vector3(300, 0, 0));
            sm.defaultState = empty;

            AddTriggeredState(sm, empty, "Shoot", animationSet.rifleShoot, "Shoot", new Vector3(520, -60, 0));
            AddTriggeredState(sm, empty, "GrenadeThrow", animationSet.grenadeThrow, "GrenadeThrow", new Vector3(520, 60, 0));
            AddTriggeredState(
                sm,
                empty,
                "Death",
                animationSet.death01 ?? animationSet.sidekickDeathForward,
                "Death",
                new Vector3(520, 180, 0),
                returnToEmpty: false);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Combat infantry animator ready at {OutputPath}. " +
                "Re-run Synty → Generate Arena Prefab Wrappers to assign it to ArenaUnit_* prefabs.");
        }

        [MenuItem("DeadManZone/Combat Arena/Assign Combat Animator To Arena Units")]
        public static void AssignCombatAnimatorToArenaUnits()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(OutputPath);
            if (controller == null)
            {
                Debug.LogError("Run Create Combat Infantry Animator first.");
                return;
            }

            string[] unitPaths =
            {
                "Assets/_Project/Art/Synty/Arena/Units/ArenaUnit_Rifle.prefab",
                "Assets/_Project/Art/Synty/Arena/Units/ArenaUnit_Support.prefab",
                "Assets/_Project/Art/Synty/Arena/Units/ArenaUnit_Medic.prefab",
                "Assets/_Project/Art/Synty/Arena/Units/ArenaUnit_Sniper.prefab",
                "Assets/_Project/Art/Synty/Arena/Units/ArenaUnit_Officer.prefab"
            };

            foreach (string path in unitPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                var animator = instance.GetComponentInChildren<Animator>();
                if (animator != null)
                    animator.runtimeAnimatorController = controller;

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Assigned AC_CombatArena_Infantry to all ArenaUnit_* prefabs.");
        }

        private static void AddTriggeredState(
            AnimatorStateMachine sm,
            AnimatorState empty,
            string stateName,
            AnimationClip clip,
            string triggerName,
            Vector3 position,
            bool returnToEmpty = true)
        {
            if (clip == null)
                return;

            var state = sm.AddState(stateName, position);
            state.motion = clip;

            var toState = empty.AddTransition(state);
            toState.hasExitTime = false;
            toState.duration = 0.06f;
            toState.AddCondition(AnimatorConditionMode.If, 0, triggerName);

            if (!returnToEmpty)
                return;

            var back = state.AddTransition(empty);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.08f;
        }

        private static void AddTriggerIfMissing(AnimatorController controller, string name)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name)
                    return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static void RemoveLayerNamed(AnimatorController controller, string layerName)
        {
            for (int i = controller.layers.Length - 1; i >= 0; i--)
            {
                if (controller.layers[i].name != layerName)
                    continue;

                controller.RemoveLayer(i);
                return;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
