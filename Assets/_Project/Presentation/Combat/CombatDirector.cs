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
        private CombatPhase _playbackSegmentPhase;

        public bool IsPlaying => _playbackRoutine != null;
        public event Action<CombatEvent> EventReplayed;
        public event Action<CombatPhase> PausedForCommands;
        public event Action CombatPresentationCompleted;

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
            if (combat.CompletedPhase == default && !combat.AwaitingCommand)
            {
                AdvanceCombatNow();
                return;
            }

            if (combat.AwaitingCommand)
                PlaySegmentFromSave(combat.CompletedPhase, CombatAdvanceStatus.AwaitingCommand);
        }

        public void PlayLog(CombatEventLog eventLog, CombatPhase segmentPhase)
        {
            PlaySegment(eventLog?.Events, segmentPhase, CombatAdvanceStatus.AwaitingCommand);
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
            PlaySegment(result.EventLog?.Events, result.CompletedPhase, result.Status);
        }

        private void PlaySegmentFromSave(CombatPhase segmentPhase, CombatAdvanceStatus status)
        {
            if (RunManager.Instance?.State?.Combat?.EventLog == null)
                return;

            var events = RunManager.Instance.State.Combat.EventLog
                .Select(e => new CombatEvent
                {
                    Phase = e.Phase,
                    Tick = e.Tick,
                    ActorId = e.ActorId,
                    ActionType = e.ActionType,
                    TargetId = e.TargetId,
                    Value = e.Value
                })
                .ToList();

            PlaySegment(events, segmentPhase, status);
        }

        private void PlaySegment(
            IEnumerable<CombatEvent> events,
            CombatPhase segmentPhase,
            CombatAdvanceStatus status)
        {
            StopPlayback();
            _playbackAdvanceStatus = status;
            _playbackSegmentPhase = segmentPhase;
            _playbackRoutine = StartCoroutine(PlaybackSegmentRoutine(events, segmentPhase, status));
        }

        private IEnumerator PlaybackSegmentRoutine(
            IEnumerable<CombatEvent> events,
            CombatPhase segmentPhase,
            CombatAdvanceStatus status)
        {
            var eventsByTick = CombatSegmentPlayback.GroupEventsByTick(segmentPhase, events);
            bool segmentEndsFight = status == CombatAdvanceStatus.Completed ||
                                    CombatSegmentPlayback.SegmentContainsFightEnd(events, segmentPhase);
            int lastTick = CombatSegmentPlayback.ResolveLastTick(segmentPhase, events, segmentEndsFight);
            bool fightEnded = false;

            for (int tick = 0; tick <= lastTick && !fightEnded; tick++)
            {
                if (eventsByTick.TryGetValue(tick, out var tickEvents))
                {
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

                if (_secondsPerTick > 0f)
                    yield return new WaitForSeconds(_secondsPerTick);
                else
                    yield return null;
            }

            _playbackRoutine = null;
            FinishPlayback();
        }

        private void FinishPlayback()
        {
            if (_continueAfterPlayback)
            {
                _continueAfterPlayback = false;
                AdvanceCombatNow();
                return;
            }

            if (_playbackAdvanceStatus == CombatAdvanceStatus.AwaitingCommand &&
                ShouldPauseAfterPlayback(_playbackSegmentPhase))
            {
                PausedForCommands?.Invoke(_playbackSegmentPhase);
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

        private static bool ShouldPauseAfterPlayback(CombatPhase segmentPhase)
        {
            var combat = RunManager.Instance?.State?.Combat;
            return combat is { AwaitingCommand: true } && combat.CompletedPhase == segmentPhase;
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
