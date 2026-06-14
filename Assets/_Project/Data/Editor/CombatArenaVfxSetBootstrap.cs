#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class CombatArenaVfxSetBootstrap
    {
        private const string AssetPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaVfxSet.asset";

        [MenuItem("DeadManZone/Combat Arena/Create Or Refresh VFX Set")]
        public static void CreateOrRefresh()
        {
            var vfxSet = AssetDatabase.LoadAssetAtPath<CombatArenaVfxSetSO>(AssetPath);
            if (vfxSet == null)
            {
                vfxSet = ScriptableObject.CreateInstance<CombatArenaVfxSetSO>();
                AssetDatabase.CreateAsset(vfxSet, AssetPath);
            }

            vfxSet.rifleMuzzle = LoadParticle(
                "Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_01.prefab");
            vfxSet.rifleMuzzleSmoke = LoadParticle(
                "Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_BarrelSmoke_01.prefab");
            vfxSet.bulletTracer = LoadParticle(
                "Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_Heavy_Single_Tracers_01.prefab");
            vfxSet.rifleImpact = LoadParticle(
                "Assets/Synty/PolygonWar/Prefabs/FX/FX_Bullet_Impact_01.prefab");
            vfxSet.cannonShot = LoadParticle(
                "Assets/Synty/PolygonWar/Prefabs/FX/FX_Cannon_Shot_01.prefab");
            vfxSet.explosionSmall = LoadParticle(
                "Assets/Synty/PolygonWar/Prefabs/FX/FX_Explosion_Small_01.prefab");
            vfxSet.explosionLarge = LoadParticle(
                "Assets/Synty/PolygonWar/Prefabs/FX/FX_Explosion_Large_01.prefab");
            vfxSet.deathBurst = LoadParticle(
                "Assets/Synty/PolygonParticleFX/Prefabs/FX_Dust_Small_01.prefab");
            vfxSet.deathSmoke = LoadParticle(
                "Assets/Synty/PolygonWar/Prefabs/FX/FX_Smoke_Small_Dark_01.prefab");

            EditorUtility.SetDirty(vfxSet);
            AssetDatabase.SaveAssets();
            Debug.Log($"Combat arena VFX set refreshed at {AssetPath}");
        }

        private static ParticleSystem LoadParticle(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"VFX prefab missing: {prefabPath}");
                return null;
            }

            return prefab.GetComponentInChildren<ParticleSystem>();
        }
    }
}
#endif
