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
        private readonly System.Collections.Generic.List<Transform> _soldierAnchors = new();
        private readonly System.Collections.Generic.List<float> _soldierScales = new();
        private Quaternion _squadFacing = Quaternion.identity;
        private float _bobTime;
        private bool _walking;
        private Coroutine _attackRoutine;
        private Coroutine _deathRoutine;
        private readonly CombatUnit2DStripPlayer _animPlayer = new();
        private bool _animated;
        private bool _dying;
        private bool _flipX;

        public bool IsBuilt => _presentationRoot != null;
        public bool BlocksLocomotion => _animated && _animPlayer.IsLocked;

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

        public void SetWalking(bool walking)
        {
            _walking = walking;
            if (_animated && !_animPlayer.IsLocked)
                _animPlayer.Play(walking ? CombatUnit2DAnimState.Walk : CombatUnit2DAnimState.Idle, restart: false);
        }

        public void FaceDirection(Vector3 worldDirection)
        {
            if (_squadRoot == null)
                return;

            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;

            // Side-view strips stay upright on billboards; 3D yaw reads as shrink/pop.
            if (_animated)
            {
                bool faceLeft = worldDirection.x < 0f;
                for (int i = 0; i < _soldierQuads.Count; i++)
                    CombatArena2DSpriteQuad.SetFlipX(_soldierQuads[i], faceLeft);
                return;
            }

            _squadRoot.rotation = _squadFacing = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        }

        public void UpdateSortAndBob(Vector3 worldPosition)
        {
            if (_presentationRoot == null)
                return;

            if (_animated)
            {
                if (!_dying)
                    TickAnimation();
            }
            else
            {
                _bobTime += Time.deltaTime * CombatUnitProceduralJog.CycleSpeed(_walking);
                float bob = CombatUnitProceduralJog.EvaluateBobY(_bobTime, _walking);
                var local = _presentationRoot.localPosition;
                _presentationRoot.localPosition = new Vector3(local.x, bob, local.z);
                ApplySquadJog();
            }

            int renderQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(worldPosition.z);
            ApplySortOrder(renderQueue);
        }

        public void PlayHurt()
        {
            // ponytail: hurt/hit-react disabled for now — combat keeps marching through damage
        }

        private void TickAnimation()
        {
            _animPlayer.Tick(Time.deltaTime);

            // Resume locomotion after a one-shot (shoot/hurt) returns to idle.
            if (!_animPlayer.IsLocked && _walking && _animPlayer.State == CombatUnit2DAnimState.Idle)
                _animPlayer.Play(CombatUnit2DAnimState.Walk, restart: false);

            Sprite frame = _animPlayer.ResolveSprite(null);
            if (frame == null)
                return;

            for (int i = 0; i < _soldierQuads.Count; i++)
                CombatArena2DSpriteQuad.SetFrame(_soldierQuads[i], frame, _soldierScales[i]);
        }

        private void ApplySquadJog()
        {
            if (_squadRoot == null)
                return;

            float lean = CombatUnitProceduralJog.EvaluateLeanDegrees(_bobTime, _walking);
            _squadRoot.rotation = _squadFacing * Quaternion.Euler(lean, 0f, 0f);
            _squadRoot.localScale = CombatUnitProceduralJog.EvaluateSquadScale(_bobTime, _walking);

            for (int i = 0; i < _soldierAnchors.Count; i++)
            {
                Transform anchor = _soldierAnchors[i];
                if (anchor == null)
                    continue;

                var offset = SquadOffsets[i];
                float anchorBob = CombatUnitProceduralJog.EvaluateAnchorBobY(_bobTime, _walking, i);
                anchor.localPosition = new Vector3(offset.x, offset.y + anchorBob, offset.z);
            }
        }

        public void PlayAttack(
            CombatAttackPresentationProfile profile,
            Vector3 targetWorld,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (_animated)
                _animPlayer.Play(CombatUnit2DAnimState.Shoot);
            if (_attackRoutine != null)
                StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackRoutine(profile, targetWorld, onMuzzle, onImpact));
        }

        public void PlayDeath(Action onComplete)
        {
            if (_deathRoutine != null)
                StopCoroutine(_deathRoutine);
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            if (_squadRoot != null)
                _squadRoot.localScale = Vector3.one;

            _walking = false;
            if (_animated)
            {
                _dying = true;
                _animPlayer.Play(CombatUnit2DAnimState.Die);
                _deathRoutine = StartCoroutine(AnimatedDeathRoutine(onComplete));
            }
            else
            {
                _deathRoutine = StartCoroutine(DeathRoutine(onComplete));
            }
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
            _soldierAnchors.Clear();
            _soldierScales.Clear();
            _squadFacing = Quaternion.identity;
            _animated = false;
            _dying = false;
            _flipX = false;
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

            // Sprites are authored facing right (toward the enemy line). The enemy
            // side advances leftward, so mirror it to face the player line.
            _flipX = side == CombatSide.Enemy;

            _animated = piece != null && piece.combatArena2DAnimations != null && piece.combatArena2DAnimations.HasAny;
            if (_animated)
            {
                _animPlayer.Bind(piece.combatArena2DAnimations);
                _animPlayer.Play(CombatUnit2DAnimState.Idle);
                sprite = _animPlayer.ResolveSprite(sprite);
                tint = Color.white;
                // Detailed character sheets are a single figure; the multi-soldier
                // squad cluster only made sense for abstract silhouettes.
                count = 1;
            }

            int baseQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(transform.position.z);

            for (int i = 0; i < count; i++)
            {
                float scale = i == 0 ? 1.05f : 0.92f;
                var anchor = new GameObject(i == 0 ? "Leader" : $"Soldier_{i}");
                anchor.transform.SetParent(_squadRoot, false);
                anchor.transform.localPosition = SquadOffsets[i];
                _soldierAnchors.Add(anchor.transform);

                var quadRoot = CombatArena2DSpriteQuad.AttachBillboard(
                    anchor.transform,
                    sprite,
                    tint,
                    scale,
                    baseQueue + i,
                    _camera);
                if (quadRoot == null)
                    continue;

                CombatArena2DSpriteQuad.SetFlipX(quadRoot, _flipX);
                _soldierQuads.Add(quadRoot);
                _soldierScales.Add(scale);
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

            onImpact?.Invoke();
            _attackRoutine = null;
        }

        private IEnumerator AnimatedDeathRoutine(Action onComplete)
        {
            float duration = Mathf.Max(1f, _animPlayer.CurrentDurationSeconds);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                TickAnimation();
                UpdateSortAndBob(transform.position);
                yield return null;
            }

            TickAnimation();
            UpdateSortAndBob(transform.position);
            _dying = false;
            onComplete?.Invoke();
            _deathRoutine = null;
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
