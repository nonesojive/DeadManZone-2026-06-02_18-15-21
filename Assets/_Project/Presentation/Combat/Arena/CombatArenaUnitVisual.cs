using System.Collections;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Instantiates a 3D combat-arena model and drives Synty locomotion or static mesh presentation.
    /// Board/shop pieces keep using 2D sprites; this is combat-arena only.
    /// </summary>
    public sealed class CombatArenaUnitVisual : MonoBehaviour
    {
        private Transform _modelRoot;
        private ICombatUnitVisualDriver _driver;
        private Coroutine _attackRoutine;

        public bool HasModel => _modelRoot != null;

        public void Build(GameObject prefab, float targetHeight, float modelScale)
        {
            Clear();

            if (prefab == null)
                return;

            var instance = Object.Instantiate(prefab, transform, false);
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

            var animator = instance.GetComponentInChildren<Animator>();
            _driver = animator != null && animator.runtimeAnimatorController != null
                ? new SyntyLocomotionVisualDriver()
                : new StaticMeshVisualDriver();
            _driver.Bind(animator);
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

        public void PlayAttackToward(Vector3 targetWorld)
        {
            if (_modelRoot == null)
                return;

            Vector3 flatTarget = new Vector3(targetWorld.x, _modelRoot.position.y, targetWorld.z);
            FaceWorldDirection(flatTarget - _modelRoot.position);

            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine());
        }

        public void Clear()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

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

        private IEnumerator AttackRoutine()
        {
            _driver?.PlayAttack();

            yield return new WaitForSeconds(0.12f);
            yield return new WaitForSeconds(0.45f);

            _attackRoutine = null;
        }
    }
}
