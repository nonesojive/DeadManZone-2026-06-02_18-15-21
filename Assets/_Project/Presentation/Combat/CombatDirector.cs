using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.Presentation.Combat
{
    public sealed class CombatDirector : MonoBehaviour
    {
        [SerializeField] private bool autoAdvanceAfterCommands = false;

        // Empty "charge" ticks between a unit's cell-steps are paced at this fraction of a full
        // tick so the grid anchor advances at ~walk speed instead of jumping a cell per event.
        // Walk speed (moveSpeedPresentationScale in the arena config) is calibrated to match this.
        private const float EmptyTickPaceScale = 0.45f;
        // Beyond this many consecutive empty ticks we stop pacing and fast-forward: real marches
        // fit inside it (slowest unit is ~50 ticks/cell), only pathological dead air is compressed.
        private const int MaxPacedEmptyTicks = 64;

        private Coroutine _playbackRoutine;
        private bool _continueAfterPlayback;
        private float _secondsPerTick = CombatSegmentPlayback.SecondsPerTick;
        private CombatAdvanceStatus _playbackAdvanceStatus;
        private int _playbackSegment;
        private PauseTriggerContext _playbackPauseTrigger;

        public bool IsPlaying => _playbackRoutine != null;
        public event Action<CombatEvent> EventReplayed;
        public event Action<PauseTriggerContext> PausedForCommands;
        public event Action CombatPresentationCompleted;
        public event Action SegmentPlaybackFinished;
        public event Action SegmentPlaybackStarting;

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced += OnCombatAdvanced;
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced -= OnCombatAdvanced;
        }

        public void PresentCombatAfterLoading()
        {
            if (RunManager.Instance?.State?.Combat == null)
                return;

            var combat = RunManager.Instance.State.Combat;
            var orchestrator = RunManager.Instance.Orchestrator;

            if (orchestrator != null && orchestrator.HasPendingCombatCompletion)
            {
                PlaySegmentFromSave(ResolveLastPlaybackSegment(combat), CombatAdvanceStatus.Completed);
                return;
            }

            if (combat.CheckpointsFired == 0
                && !combat.AwaitingCommand
                && (combat.EventLog == null || combat.EventLog.Count == 0))
            {
                AdvanceCombatNow();
                return;
            }

            if (combat.AwaitingCommand)
            {
                PlaySegmentFromSave(combat.LastSegmentIndex, CombatAdvanceStatus.AwaitingCommand);
                return;
            }

            AdvanceCombatNow();
        }

        private static int ResolveLastPlaybackSegment(CombatSaveState combat)
        {
            if (combat?.EventLog == null || combat.EventLog.Count == 0)
                return combat?.LastSegmentIndex ?? 0;

            int maxSegment = combat.LastSegmentIndex;
            for (int i = 0; i < combat.EventLog.Count; i++)
            {
                int segment = combat.EventLog[i].Segment;
                if (segment > maxSegment)
                    maxSegment = segment;
            }

            return maxSegment;
        }

        public void PlayLog(CombatEventLog eventLog, int segment)
        {
            PlaySegment(eventLog?.Events, segment, CombatAdvanceStatus.AwaitingCommand);
        }

        public void ContinueCombat()
        {
            if (RunManager.Instance == null)
                return;

            if (IsPlaying)
            {
                _continueAfterPlayback = true;
                return;
            }

            AdvanceCombatNow();
        }

        public void SetSecondsPerTickForTests(float seconds) => _secondsPerTick = seconds;

        private void OnCombatAdvanced(CombatAdvanceResult result)
        {
            _playbackPauseTrigger = result.PauseTrigger;
            SegmentPlaybackStarting?.Invoke();
            PlaySegment(result.EventLog?.Events, result.SegmentIndex, result.Status);
        }

        private void PlaySegmentFromSave(int segment, CombatAdvanceStatus status)
        {
            if (RunManager.Instance?.State?.Combat?.EventLog == null)
                return;

            var events = RunManager.Instance.State.Combat.EventLog
                .Select(e => new CombatEvent
                {
                    Segment = e.Segment,
                    Tick = e.Tick,
                    ActorId = e.ActorId,
                    ActionType = e.ActionType,
                    TargetId = e.TargetId,
                    Value = e.Value
                })
                .ToList();

            PlaySegment(events, segment, status);
        }

        private void PlaySegment(
            IEnumerable<CombatEvent> events,
            int segment,
            CombatAdvanceStatus status)
        {
            StopPlayback();
            _playbackAdvanceStatus = status;
            _playbackSegment = segment;
            _playbackRoutine = StartCoroutine(PlaybackSegmentRoutine(events, segment, status));
        }

        private IEnumerator PlaybackSegmentRoutine(
            IEnumerable<CombatEvent> events,
            int segment,
            CombatAdvanceStatus status)
        {
            var eventsByTick = CombatSegmentPlayback.GroupEventsByTick(segment, events);
            bool segmentEndsFight = status == CombatAdvanceStatus.Completed ||
                                    CombatSegmentPlayback.SegmentContainsFightEnd(events, segment);
            int firstTick = CombatSegmentPlayback.ResolveFirstTick(segment, events);
            int lastTick = CombatSegmentPlayback.ResolveLastTick(segment, events);
            if (segmentEndsFight && lastTick < 0)
                lastTick = 0;

            if (firstTick < 0)
            {
                _playbackRoutine = null;
                FinishPlayback();
                yield break;
            }

            bool fightEnded = false;
            int emptyRun = 0;

            for (int tick = firstTick; tick <= lastTick && !fightEnded; tick++)
            {
                bool hadEvents = false;
                if (eventsByTick.TryGetValue(tick, out var tickEvents))
                {
                    hadEvents = true;
                    foreach (var combatEvent in tickEvents)
                    {
                        EventReplayed?.Invoke(combatEvent);
                        if (combatEvent.ActionType == "fight_end")
                        {
                            fightEnded = true;
                            break;
                        }
                    }
                }

                if (fightEnded)
                    break;

                if (_secondsPerTick <= 0f)
                    continue;

                // A unit takes many "charge" ticks to cross one cell (100/(speed+1) ≈ 20-50),
                // and most produce no event. The old code skipped every empty tick, so a lone
                // marching unit's grid anchor jumped a full cell per *event* tick — tens of times
                // faster than its calibrated walk speed. The visual couldn't keep up and caught
                // up in a lunge. Pace the empty charge ticks too (at a fraction of a full tick)
                // so the anchor advances at ~walk speed; event ticks keep full pacing so attack
                // and volley timing is unchanged. A cap still compresses pathological dead air.
                if (hadEvents)
                {
                    emptyRun = 0;
                    yield return new WaitForSeconds(_secondsPerTick);
                }
                else if (emptyRun < MaxPacedEmptyTicks)
                {
                    emptyRun++;
                    yield return new WaitForSeconds(_secondsPerTick * EmptyTickPaceScale);
                }
            }

            _playbackRoutine = null;
            FinishPlayback();
        }

        private void FinishPlayback()
        {
            SegmentPlaybackFinished?.Invoke();

            if (_continueAfterPlayback)
            {
                _continueAfterPlayback = false;
                AdvanceCombatNow();
                return;
            }

            if (_playbackAdvanceStatus == CombatAdvanceStatus.AwaitingCommand &&
                ShouldPauseAfterPlayback(_playbackSegment))
            {
                var trigger = _playbackPauseTrigger
                    ?? RunManager.Instance?.Orchestrator?.GetCombatPauseContext()?.Trigger;
                PausedForCommands?.Invoke(trigger);
                return;
            }

            if (_playbackAdvanceStatus == CombatAdvanceStatus.Completed)
            {
                CombatPresentationCompleted?.Invoke();
                return;
            }

            if (autoAdvanceAfterCommands &&
                RunManager.Instance?.State?.Combat?.AwaitingCommand == false &&
                RunManager.Instance.State.Phase == RunPhase.Combat)
            {
                AdvanceCombatNow();
            }
        }

        private static bool ShouldPauseAfterPlayback(int segment)
        {
            var combat = RunManager.Instance?.State?.Combat;
            return combat is { AwaitingCommand: true } && combat.LastSegmentIndex == segment;
        }

        private void AdvanceCombatNow()
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.AdvanceCombat();
        }

        private void StopPlayback()
        {
            if (_playbackRoutine == null)
                return;

            StopCoroutine(_playbackRoutine);
            _playbackRoutine = null;
        }
    }
}
