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

                // ponytail: sim advances every tick; presentation only paces ticks that produced visible events
                if (hadEvents && _secondsPerTick > 0f)
                    yield return new WaitForSeconds(_secondsPerTick);
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
