using System;
using System.Collections;
using DeadManZone.Core.Combat;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Sprite-based unit presentation for Top Troops 2D arena mode.</summary>
    public sealed class CombatUnitVisual2D : MonoBehaviour
    {
        private static readonly Vector3[] SquadOffsets =
        {
            new(-0.22f, 0f, -0.15f),
            new(0.22f, 0f, -0.15f),
            new(-0.22f, 0f, 0.15f),
            new(0.22f, 0f, 0.15f),
            new(0f, 0f, 0f)
        };

        private Transform _presentationRoot;
        private Transform _squadRoot;
        private Camera _camera;
        private readonly System.Collections.Generic.List<GameObject> _soldierQuads = new();
        private float _bobTime;
        private bool _walking;
        private Coroutine _attackRoutine;
        private Coroutine _deathRoutine;

        public bool IsBuilt => _presentationRoot != null;

        public void Build(
            PieceDefinitionSO piece,
            CombatSide side,
            Camera arenaCamera,
            int squadSize)
        {
            Clear();
            _camera = arenaCamera;

            var rootGo = new GameObject("PresentationRoot");
            rootGo.transform.SetParent(transform, false);
            _presentationRoot = rootGo.transform;

            CreateShadow();
            CreateSquad(piece, side, Mathf.Clamp(squadSize, 1, SquadOffsets.Length));
        }

        public void SetWalking(bool walking) => _walking = walking;

        public void FaceDirection(Vector3 worldDirection)
        {
            if (_squadRoot == null)
                return;

            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;

            _squadRoot.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        }

        public void UpdateSortAndBob(Vector3 worldPosition)
        {
            if (_presentationRoot == null)
                return;

            _bobTime += Time.deltaTime * (_walking ? 10f : 6f);
            float bobAmp = _walking ? 0.04f : 0.02f;
            float bob = Mathf.Sin(_bobTime) * bobAmp;
            var local = _presentationRoot.localPosition;
            _presentationRoot.localPosition = new Vector3(local.x, bob, local.z);

            int renderQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(worldPosition.z);
            ApplySortOrder(renderQueue);
        }

        public void PlayAttack(
            CombatAttackPresentationProfile profile,
            Vector3 targetWorld,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine(profile, targetWorld, onMuzzle, onImpact));
        }

        public void PlayDeath(Action onComplete)
        {
            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _deathRoutine = StartCoroutine(DeathRoutine(onComplete));
        }

        public void Clear()
        {
            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            _attackRoutine = null;
            _deathRoutine = null;

            if (_presentationRoot != null)
                Destroy(_presentationRoot.gameObject);
            _presentationRoot = null;
            _squadRoot = null;
            _soldierQuads.Clear();
        }

        private void CreateShadow()
        {
            int renderQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(transform.position.z) - 1;
            CombatArena2DSpriteQuad.CreateFlatShadow(
                _presentationRoot,
                CombatArena2DEnvironmentArt.UnitShadow,
                new Vector3(0f, 0.02f, -0.08f),
                Vector3.one * 0.55f,
                renderQueue);
        }

        private void CreateSquad(PieceDefinitionSO piece, CombatSide side, int count)
        {
            var squadGo = new GameObject("SquadRoot");
            squadGo.transform.SetParent(_presentationRoot, false);
            squadGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            _squadRoot = squadGo.transform;

            Sprite sprite = CombatUnitSpriteResolver.Resolve(piece, side);
            Color tint = CombatUnitSpriteResolver.ResolveTint(piece, side);

            int baseQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(transform.position.z);

            for (int i = 0; i < count; i++)
            {
                float scale = i == 0 ? 1.05f : 0.92f;
                var anchor = new GameObject(i == 0 ? "Leader" : $"Soldier_{i}");
                anchor.transform.SetParent(_squadRoot, false);
                anchor.transform.localPosition = SquadOffsets[i];

                var quadRoot = CombatArena2DSpriteQuad.AttachBillboard(
                    anchor.transform,
                    sprite,
                    tint,
                    scale,
                    baseQueue + i,
                    _camera);
                if (quadRoot == null)
                    continue;

                _soldierQuads.Add(quadRoot);
            }
        }

        private void ApplySortOrder(int baseRenderQueue)
        {
            for (int i = 0; i < _soldierQuads.Count; i++)
                CombatArena2DSpriteQuad.SetRenderQueue(_soldierQuads[i], baseRenderQueue + i);
        }

        private IEnumerator AttackRoutine(
            CombatAttackPresentationProfile profile,
            Vector3 targetWorld,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            Vector3 muzzle = transform.position + Vector3.up * 0.45f + (_squadRoot != null ? _squadRoot.forward : transform.forward) * 0.2f;
            onMuzzle?.Invoke(muzzle);

            float impactWait = profile.ImpactDelaySeconds - profile.MuzzleDelaySeconds;
            if (impactWait > 0f)
                yield return new WaitForSeconds(impactWait);

            if (_squadRoot != null)
                _squadRoot.localScale = Vector3.one * 0.94f;

            onImpact?.Invoke();
            _attackRoutine = null;
        }

        private IEnumerator DeathRoutine(Action onComplete)
        {
            Transform target = _presentationRoot != null ? _presentationRoot : transform;
            Vector3 start = target.localScale;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                float p = t / 0.4f;
                target.localScale = Vector3.Lerp(start, Vector3.zero, p);
                yield return null;
            }

            onComplete?.Invoke();
            _deathRoutine = null;
        }
    }
}
