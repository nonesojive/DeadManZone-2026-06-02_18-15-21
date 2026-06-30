using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Drives Top Troops-style free world-space chase toward engagement goals each frame.
    /// Sim grid anchors remain authoritative for async PvP replay; this is presentation only.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class CombatArenaChaseController : MonoBehaviour
    {
        private CombatArenaPresenter _presenter;
        private CombatReplayState _replayState;
        private CombatGridMapper _mapper;
        private BattlefieldState _battlefield;
        private CombatArenaConfigSO _config;
        private CombatDirector _combatDirector;

        public void Configure(
            CombatArenaPresenter presenter,
            CombatReplayState replayState,
            CombatGridMapper mapper,
            BattlefieldState battlefield,
            CombatArenaConfigSO config,
            CombatDirector combatDirector = null)
        {
            _presenter = presenter;
            _replayState = replayState;
            _mapper = mapper;
            _battlefield = battlefield;
            _config = config;
            _combatDirector = combatDirector;
        }

        public void Clear()
        {
            _presenter = null;
            _replayState = null;
            _mapper = null;
            _battlefield = null;
            _config = null;
            _combatDirector = null;
        }

        private void Awake()
        {
            if (_combatDirector == null)
                _combatDirector = GetComponent<CombatDirector>();
        }

        private void Update()
        {
            if (_presenter == null
                || _replayState == null
                || _mapper == null
                || _battlefield == null
                || _config == null
                || !_config.useTopTroopsFreeChaseMovement
                || _presenter.IsPresentationFrozen)
            {
                return;
            }

            if (_combatDirector == null)
                _combatDirector = GetComponent<CombatDirector>();

            if (_combatDirector != null && !_combatDirector.IsPlaying)
                return;

            var anchors = _replayState.Anchors;
            var cells = _battlefield.Cells;

            foreach (var actor in _presenter.GetActiveActors())
            {
                if (actor == null || !actor.IsAlive)
                    continue;

                var cell = _battlefield.FindCell(actor.InstanceId);
                if (cell?.Definition == null)
                {
                    actor.ClearChaseTarget();
                    continue;
                }

                if (!anchors.TryGetValue(actor.InstanceId, out var anchor))
                {
                    actor.ClearChaseTarget();
                    continue;
                }

                if (!CombatPresentationEngagement.ShouldChase(cell, anchor, cells, anchors, _battlefield.Layout))
                {
                    actor.ClearChaseTarget();
                    continue;
                }

                var goal = CombatPresentationEngagement.ComputeChaseAnchor(
                    cell,
                    anchor,
                    cells,
                    anchors,
                    _battlefield.Layout,
                    _config.topTroopsChaseMaxLeadCells);

                actor.SetChaseTargetWorld(_mapper.ToWorld(goal));
            }
        }
    }
}
