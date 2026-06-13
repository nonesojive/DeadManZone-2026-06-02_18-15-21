using System.Collections;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Instantiates a 3D combat-arena model and drives POLYGON WW2 German soldier animators.
    /// Board/shop pieces keep using 2D sprites; this is combat-arena only.
    /// </summary>
    public sealed class CombatArenaUnitVisual : MonoBehaviour
    {
        private static readonly int StatusWalk = Animator.StringToHash("Status_walk");
        private static readonly int StatusK98 = Animator.StringToHash("status_k98");
        private static readonly int StatusStg44 = Animator.StringToHash("Status_stg44");
        private static readonly int StatusMp40 = Animator.StringToHash("status_MP40");
        private static readonly int StatusLuger = Animator.StringToHash("Status_LugerP08");
        private static readonly int StatusPanzerschreck = Animator.StringToHash("Status_panzerschreck");
        private static readonly int StatusPanzerfaust = Animator.StringToHash("Status_panzerfaust");
        private static readonly int StatusMg42 = Animator.StringToHash("Status_MG42");
        private static readonly int StatusFlammenwerfer = Animator.StringToHash("Flammenwerfer_status");

        private static readonly string[] HiddenWeaponObjectNames =
        {
            "STG44", "MG42", "MP40", "LugerP08", "Panzerschreck", "Panzerfaust", "Flammenwerfer"
        };

        private Transform _modelRoot;
        private Animator _animator;
        private Coroutine _attackRoutine;
        private bool _walking;

        public bool HasModel => _modelRoot != null;

        public void Build(GameObject prefab, float targetHeight, float modelScale)
        {
            Clear();

            if (prefab == null)
                return;

            // Legacy store prefabs must reference the root GameObject fileID, not the Prefab wrapper.
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

            FitHeight(targetHeight);

            float scale = modelScale > 0f ? modelScale : 1f;
            if (Mathf.Abs(scale - 1f) > 0.001f)
                _modelRoot.localScale *= scale;
            _animator = instance.GetComponentInChildren<Animator>();
            HideUnusedWeapons(instance.transform);
            SetIdleWithK98();
        }

        public void SetWalking(bool walking)
        {
            if (_walking == walking || _animator == null)
                return;

            _walking = walking;
            _animator.SetInteger(StatusWalk, walking ? 1 : 0);
        }

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
            if (_animator == null || _modelRoot == null)
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

            _walking = false;
            _animator = null;

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

        private static void HideUnusedWeapons(Transform root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                string objectName = renderer.gameObject.name;
                for (int i = 0; i < HiddenWeaponObjectNames.Length; i++)
                {
                    if (objectName != HiddenWeaponObjectNames[i])
                        continue;

                    renderer.gameObject.SetActive(false);
                    break;
                }
            }
        }

        private void SetIdleWithK98()
        {
            if (_animator == null)
                return;

            ZeroWeaponLayers();
            _animator.SetInteger(StatusK98, 1);
            _animator.SetInteger(StatusWalk, 0);
        }

        private void ZeroWeaponLayers()
        {
            if (_animator == null)
                return;

            _animator.SetInteger(StatusStg44, 0);
            _animator.SetInteger(StatusMp40, 0);
            _animator.SetInteger(StatusLuger, 0);
            _animator.SetInteger(StatusPanzerschreck, 0);
            _animator.SetInteger(StatusPanzerfaust, 0);
            _animator.SetInteger(StatusMg42, 0);
            _animator.SetInteger(StatusFlammenwerfer, 0);
        }

        private IEnumerator AttackRoutine()
        {
            SetWalking(false);
            _animator.SetInteger(StatusK98, 2);

            yield return new WaitForSeconds(0.12f);

            _animator.SetInteger(StatusK98, 4);

            yield return new WaitForSeconds(0.45f);

            _animator.SetInteger(StatusK98, 1);
            _attackRoutine = null;
        }
    }
}
