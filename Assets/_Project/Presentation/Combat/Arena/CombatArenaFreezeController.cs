using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaFreezeController : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private CombatArenaPresenter arenaPresenter;

        private readonly List<ParticleSystem> _trackedParticles = new();
        private bool _frozen;

        private void OnEnable()
        {
            EnsureReferences();

            if (combatDirector != null)
                combatDirector.PausedForCommands += OnPausedForCommands;

            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced += OnCombatAdvanced;
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.PausedForCommands -= OnPausedForCommands;

            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced -= OnCombatAdvanced;
        }

        public void Resume() => SetFrozen(false);

        public void Configure(CombatDirector director, CombatArenaPresenter presenter)
        {
            if (director != null)
                combatDirector = director;

            if (presenter != null)
                arenaPresenter = presenter;
        }

        public void TrackParticle(ParticleSystem particle)
        {
            if (particle == null)
                return;

            _trackedParticles.RemoveAll(p => p == null);
            if (!_trackedParticles.Contains(particle))
                _trackedParticles.Add(particle);

            if (_frozen)
                particle.Pause(true);
        }

        private void OnPausedForCommands(PauseTriggerContext _) => SetFrozen(true);

        private void OnCombatAdvanced(CombatAdvanceResult _) => Resume();

        private void SetFrozen(bool frozen)
        {
            _frozen = frozen;
            SetActorFreeze(frozen);
            SetParticlesPaused(frozen);
        }

        private void SetActorFreeze(bool frozen)
        {
            if (arenaPresenter == null)
                return;

            foreach (var actor in arenaPresenter.GetActiveActors())
                actor.SetFrozen(frozen);
        }

        private void SetParticlesPaused(bool paused)
        {
            _trackedParticles.RemoveAll(p => p == null);

            foreach (var particle in _trackedParticles)
            {
                if (particle == null)
                    continue;

                if (paused)
                {
                    if (particle.isPlaying)
                        particle.Pause(true);
                }
                else if (particle.isPaused)
                {
                    particle.Play(true);
                }
            }
        }

        private void EnsureReferences()
        {
            if (combatDirector == null)
                combatDirector = GetComponent<CombatDirector>();

            if (arenaPresenter == null)
                arenaPresenter = GetComponent<CombatArenaPresenter>();
        }
    }
}
