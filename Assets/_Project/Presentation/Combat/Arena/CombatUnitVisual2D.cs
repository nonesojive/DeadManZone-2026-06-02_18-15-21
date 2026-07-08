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
        /// <summary>Die strips are authored ~4s; compress to a snappy fall that still reads.</summary>
        public const float DieStripSeconds = 1.2f;

        /// <summary>Hold the corpse's final frame briefly before the actor is pooled.</summary>
        public const float CorpseLingerSeconds = 0.45f;

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
        private Coroutine _hurtRoutine;
        private Color _baseTint = Color.white;
        private readonly CombatUnit2DStripPlayer _animPlayer = new();
        private bool _animated;
        private bool _dying;
        private bool _flipX;
        private float _locomotionLockUntil;
        private Sprite _lastFrame;
        private int _lastRenderQueue = int.MinValue;
        private float _visualHeight = 1.8f;

        public bool IsBuilt => _presentationRoot != null;

        /// <summary>Approx world height of the rendered figure (feet→head), for
        /// positioning head-height UI (health bars) and shoulder-height VFX (muzzle).</summary>
        public float VisualHeight => _visualHeight;
        // ponytail: shoot/die strips can run longer than sim attack cadence; lock locomotion
        // only through the presentation profile window (or full die), not the whole strip.
        public bool BlocksLocomotion => _animated && (_dying || Time.time < _locomotionLockUntil);

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

            CreateSquad(piece, side, Mathf.Clamp(squadSize, 1, SquadOffsets.Length));
            CreateShadow();
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
                _flipX = faceLeft;
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

        /// <summary>Brief damage flash; combat keeps marching, but hits must read on the victim.</summary>
        public void PlayHurt()
        {
            if (_dying || _soldierQuads.Count == 0)
                return;

            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            _hurtRoutine = StartCoroutine(HurtFlashRoutine());
        }

        private IEnumerator HurtFlashRoutine()
        {
            var flash = new Color(1f, 0.42f, 0.36f, 1f);
            for (int i = 0; i < _soldierQuads.Count; i++)
                CombatArena2DSpriteQuad.SetTint(_soldierQuads[i], flash);

            yield return new WaitForSeconds(0.09f);

            for (int i = 0; i < _soldierQuads.Count; i++)
                CombatArena2DSpriteQuad.SetTint(_soldierQuads[i], _baseTint);
            _hurtRoutine = null;
        }

        private void TickAnimation()
        {
            _animPlayer.Tick(Time.deltaTime);

            // Resume locomotion after a one-shot (shoot/hurt) returns to idle.
            if (!_animPlayer.IsLocked && _walking && _animPlayer.State == CombatUnit2DAnimState.Idle)
                _animPlayer.Play(CombatUnit2DAnimState.Walk, restart: false);

            Sprite frame = _animPlayer.ResolveSprite(null);
            if (frame == null || frame == _lastFrame)
                return;

            for (int i = 0; i < _soldierQuads.Count; i++)
                CombatArena2DSpriteQuad.SetFrame(_soldierQuads[i], frame, _soldierScales[i]);

            _lastFrame = frame;
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
            {
                // Square up to the target before firing; shooting backwards reads as a glitch.
                FaceDirection(targetWorld - transform.position);

                // Let an in-flight shoot finish instead of twitch-restarting on every
                // damage event; the strip is compressed to end exactly with the lock.
                bool shootInProgress = _animPlayer.State == CombatUnit2DAnimState.Shoot && _animPlayer.IsLocked;
                if (!shootInProgress)
                {
                    float window = Mathf.Max(0.05f, profile.TotalDurationSeconds);
                    _animPlayer.Play(CombatUnit2DAnimState.Shoot, restart: true, targetDurationSeconds: window);
                    _locomotionLockUntil = Time.time + window;
                }
            }
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

            if (_hurtRoutine != null)
            {
                StopCoroutine(_hurtRoutine);
                _hurtRoutine = null;
                for (int i = 0; i < _soldierQuads.Count; i++)
                    CombatArena2DSpriteQuad.SetTint(_soldierQuads[i], _baseTint);
            }

            if (_squadRoot != null)
                _squadRoot.localScale = Vector3.one;

            _walking = false;
            if (_animated)
            {
                _dying = true;
                _locomotionLockUntil = float.MaxValue;
                _animPlayer.Play(CombatUnit2DAnimState.Die, restart: true, targetDurationSeconds: DieStripSeconds);
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
            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            _attackRoutine = null;
            _deathRoutine = null;
            _hurtRoutine = null;
            _baseTint = Color.white;

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
            _locomotionLockUntil = 0f;
            _lastFrame = null;
            _lastRenderQueue = int.MinValue;
        }

        private void CreateShadow()
        {
            int renderQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(transform.position.z) - 1;
            // Ground cells sit at y≈0.07 now; keep the blob just above them so it isn't
            // occluded, and scale it to the figure so bigger units cast bigger shadows.
            float footprint = Mathf.Clamp(_visualHeight * 0.42f, 0.5f, 1.3f);
            CombatArena2DSpriteQuad.CreateFlatShadow(
                _presentationRoot,
                CombatArena2DEnvironmentArt.UnitShadow,
                new Vector3(0f, 0.09f, -0.05f),
                new Vector3(footprint, footprint * 0.55f, 1f),
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
                _lastFrame = sprite;
                tint = Color.white;
                // Detailed character sheets are a single figure; the multi-soldier
                // squad cluster only made sense for abstract silhouettes.
                count = 1;
            }

            _baseTint = tint;
            int baseQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(transform.position.z);

            // Figure top ≈ squad-root offset + visible sprite height (feet→head).
            float leaderScale = CombatUnit2DVisualScale.ResolveUniformScale(piece, sprite);
            _visualHeight = 0.35f + CombatArena2DSpriteMetrics.VisibleHeightUnits(sprite) * leaderScale;

            for (int i = 0; i < count; i++)
            {
                float scale = CombatUnit2DVisualScale.ResolveUniformScale(piece, sprite);
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
                    _camera,
                    outline: true);
                if (quadRoot == null)
                    continue;

                CombatArena2DSpriteQuad.SetFlipX(quadRoot, _flipX);
                _soldierQuads.Add(quadRoot);
                _soldierScales.Add(scale);
            }
        }

        private void ApplySortOrder(int baseRenderQueue)
        {
            if (baseRenderQueue == _lastRenderQueue)
                return;

            for (int i = 0; i < _soldierQuads.Count; i++)
                CombatArena2DSpriteQuad.SetRenderQueue(_soldierQuads[i], baseRenderQueue + i);

            _lastRenderQueue = baseRenderQueue;
        }

        private IEnumerator AttackRoutine(
            CombatAttackPresentationProfile profile,
            Vector3 targetWorld,
            Action<Vector3> onMuzzle,
            Action onImpact)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            // Animated sprites face by flip-X, so the barrel is lateral, not squad-forward.
            // Shoulder height scales with the figure — a fixed 0.72 sat at the hip once
            // units grew, so tracers came out of the legs.
            float shoulder = Mathf.Max(0.8f, _visualHeight * 0.72f);
            float lateral = Mathf.Max(0.35f, _visualHeight * 0.28f);
            Vector3 barrelOffset = _animated
                ? (_flipX ? Vector3.left : Vector3.right) * lateral
                : (_squadRoot != null ? _squadRoot.forward : transform.forward) * 0.2f;
            Vector3 muzzle = transform.position + Vector3.up * shoulder + barrelOffset;
            onMuzzle?.Invoke(muzzle);

            float impactWait = profile.ImpactDelaySeconds - profile.MuzzleDelaySeconds;
            if (impactWait > 0f)
                yield return new WaitForSeconds(impactWait);

            onImpact?.Invoke();
            _attackRoutine = null;
        }

        private IEnumerator AnimatedDeathRoutine(Action onComplete)
        {
            float duration = Mathf.Max(0.3f, _animPlayer.CurrentDurationSeconds);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                TickAnimation();
                UpdateSortAndBob(transform.position);
                yield return null;
            }

            TickAnimation();
            UpdateSortAndBob(transform.position);

            // Corpse holds its last frame, then fades out instead of popping off.
            if (CorpseLingerSeconds > 0f)
            {
                float holdSeconds = CorpseLingerSeconds * 0.4f;
                float fadeSeconds = CorpseLingerSeconds - holdSeconds;
                yield return new WaitForSeconds(holdSeconds);

                if (_lastFrame != null)
                {
                    int queue = CombatArena2DSortOrder.RenderQueueFromWorldZ(transform.position.z);
                    for (int i = 0; i < _soldierQuads.Count; i++)
                        CombatArena2DSpriteQuad.SetFadeMaterial(_soldierQuads[i], _lastFrame, queue);
                }

                for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
                {
                    float alpha = 1f - Mathf.Clamp01(t / fadeSeconds);
                    var fade = new Color(1f, 1f, 1f, alpha);
                    for (int i = 0; i < _soldierQuads.Count; i++)
                        CombatArena2DSpriteQuad.SetTint(_soldierQuads[i], fade);
                    yield return null;
                }
            }

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
