using System;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Instantiates a 3D combat-arena model and drives humanoid or vehicle combat presentation.
    /// Board/shop pieces keep using 2D sprites; this is combat-arena only.
    /// </summary>
    public sealed class CombatArenaUnitVisual : MonoBehaviour
    {
        private Transform _modelRoot;
        private ICombatUnitVisualDriver _driver;

        public bool HasModel => _modelRoot != null;

        public void Build(GameObject prefab, float targetHeight, float modelScale)
        {
            Clear();

            if (prefab == null)
                return;

            var instance = UnityEngine.Object.Instantiate(prefab, transform, false);
            if (instance == null)
            {
                Debug.LogError($"Failed to instantiate combat arena prefab '{prefab.name}'.");
                return;
            }

            instance.name = prefab.name;
            _modelRoot = instance.transform;
            _modelRoot.localPosition = Vector3.zero;
            _modelRoot.localRotation = Quaternion.identity;
            _modelRoot.localScale = Vector3.one;

            CombatArenaFxCull.RemoveTransparentFxRenderers(instance);

            FitHeight(targetHeight);

            float scale = modelScale > 0f ? modelScale : 1f;
            if (Mathf.Abs(scale - 1f) > 0.001f)
                _modelRoot.localScale *= scale;

            var animationSet = Resources.Load<CombatArenaAnimationSetSO>("DeadManZone/CombatArenaAnimationSet");
            var animator = instance.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                var humanoidDriver = new HumanoidCombatVisualDriver();
                humanoidDriver.Configure(this, _modelRoot);
                _driver = humanoidDriver;
                _driver.Bind(animator, animationSet);
            }
            else
            {
                var vehicleDriver = new VehicleCombatVisualDriver();
                vehicleDriver.Configure(this, _modelRoot);
                _driver = vehicleDriver;
                _driver.Bind(null, animationSet);
            }
        }

        public void SetWalking(bool walking) => _driver?.SetWalking(walking);

        public void FaceWorldDirection(Vector3 worldDirection)
        {
            if (_modelRoot == null)
                return;

            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;

            _modelRoot.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        }

        public void PlayAttackToward(
            Vector3 targetWorld,
            CombatAttackPresentationProfile profile,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (_modelRoot == null)
                return;

            Vector3 flatTarget = new Vector3(targetWorld.x, _modelRoot.position.y, targetWorld.z);
            FaceWorldDirection(flatTarget - _modelRoot.position);

            _driver?.PlayAttack(
                profile,
                () => onMuzzle?.Invoke(_driver.GetMuzzleWorldPosition()),
                onImpact);
        }

        public void PlayDeath(Action onComplete) => _driver?.PlayDeath(onComplete);

        public void Clear()
        {
            _driver?.Clear();
            _driver = null;

            if (_modelRoot != null)
            {
                Destroy(_modelRoot.gameObject);
                _modelRoot = null;
            }
        }

        private void FitHeight(float targetHeight)
        {
            if (_modelRoot == null || targetHeight <= 0f)
                return;

            var renderers = _modelRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float currentHeight = bounds.size.y;
            if (currentHeight <= 0.001f)
                return;

            _modelRoot.localScale *= targetHeight / currentHeight;
        }
    }
}
