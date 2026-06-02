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
        [SerializeField] private float tickDelaySeconds = 0.15f;
        [SerializeField] private bool autoAdvanceAfterCommands = true;

        private Coroutine _playbackRoutine;
        private bool _continueAfterPlayback;

        public bool IsPlaying => _playbackRoutine != null;
        public event Action<CombatEvent> EventReplayed;
        public event Action<CombatPhase> PausedForCommands;

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

        public void PlayLog(CombatEventLog eventLog, CombatPhase upToPhase)
        {
            StopPlayback();
            _playbackRoutine = StartCoroutine(PlaybackRoutine(eventLog?.Events, upToPhase));
        }

        public void ReplayFromSaveAndPauseAtCheckpoint()
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

            var log = new CombatEventLog();
            foreach (var e in events)
                log.Append(e.Phase, e.Tick, e.ActorId, e.ActionType, e.TargetId, e.Value);

            PlayLog(log, RunManager.Instance.State.Combat.CompletedPhase);
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

        public void SetTickDelayForTests(float delay) => tickDelaySeconds = delay;

        private void OnCombatAdvanced(CombatAdvanceResult result)
        {
            PlayLog(result.EventLog, result.CompletedPhase);

            if (result.Status == CombatAdvanceStatus.AwaitingCommand)
                PausedForCommands?.Invoke(result.CompletedPhase);
        }

        private IEnumerator PlaybackRoutine(IEnumerable<CombatEvent> events, CombatPhase upToPhase)
        {
            if (events != null)
            {
                var filtered = events.Where(e => e.Phase <= upToPhase)
                    .OrderBy(e => e.Phase)
                    .ThenBy(e => e.Tick)
                    .ToList();

                foreach (var combatEvent in filtered)
                {
                    EventReplayed?.Invoke(combatEvent);
                    if (tickDelaySeconds > 0f)
                        yield return new WaitForSeconds(tickDelaySeconds);
                    else
                        yield return null;
                }
            }

            _playbackRoutine = null;

            if (_continueAfterPlayback)
            {
                _continueAfterPlayback = false;
                AdvanceCombatNow();
                yield break;
            }

            if (autoAdvanceAfterCommands &&
                RunManager.Instance?.State?.Combat?.AwaitingCommand == false &&
                RunManager.Instance.State.Phase == RunPhase.Combat)
            {
                AdvanceCombatNow();
            }
        }

        private void AdvanceCombatNow()
        {
            if (RunManager.Instance == null)
                return;

            var result = RunManager.Instance.AdvanceCombat();
            if (result.Status == CombatAdvanceStatus.AwaitingCommand)
                PausedForCommands?.Invoke(result.CompletedPhase);
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
