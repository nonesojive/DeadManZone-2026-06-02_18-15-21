using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Drives the two army health bars from replayed combat events (not sim state),
    /// so the bars fall in sync with the fight the player is watching.
    /// Event subscription is owned by <see cref="CombatFlowPresenter"/>.
    /// </summary>
    public sealed class ArmyHealthBarPresenter : MonoBehaviour
    {
        [SerializeField] private ArmyHealthBarView playerBar;
        [SerializeField] private ArmyHealthBarView enemyBar;

        private readonly ArmyHealthReplayTracker _tracker = new();

        private void Awake() => ResolveViewReferences();

        public void BindViews(ArmyHealthBarView player, ArmyHealthBarView enemy)
        {
            playerBar = player;
            enemyBar = enemy;
        }

        /// <summary>Register all Combatant-tagged units at full HP and snap bars to 100%.</summary>
        public void InitializeFromBattlefield(BattlefieldState battlefield)
        {
            ResolveViewReferences();
            _tracker.Clear();
            if (battlefield != null)
            {
                foreach (var cell in battlefield.Cells)
                {
                    if (cell?.Definition == null)
                        continue;

                    if (!PieceCombatRules.ParticipatesInCombat(cell.Definition))
                        continue;

                    _tracker.RegisterUnit(cell.InstanceId, cell.Side, cell.Definition.MaxHp);
                }
            }

            SnapBars();
        }

        /// <summary>Apply a saved event without tweening (save/resume restore path).</summary>
        public void ApplyEventStateOnly(CombatEvent combatEvent) => _tracker.ApplyEvent(combatEvent);

        /// <summary>Called by CombatFlowPresenter for each replayed combat event.</summary>
        public void HandleReplayEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null)
                return;

            _tracker.ApplyEvent(combatEvent);
            RefreshBarFractions();
        }

        public void SnapBars()
        {
            ResolveViewReferences();
            playerBar?.SetFractionImmediate(_tracker.GetFraction(CombatSide.Player));
            enemyBar?.SetFractionImmediate(_tracker.GetFraction(CombatSide.Enemy));
        }

        public void InitializeForTests(
            ArmyHealthBarView player,
            ArmyHealthBarView enemy)
        {
            playerBar = player;
            enemyBar = enemy;
        }

        public void RegisterUnitForTests(string instanceId, CombatSide side, int maxHp) =>
            _tracker.RegisterUnit(instanceId, side, maxHp);

        public float GetTrackedFractionForTests(CombatSide side) => _tracker.GetFraction(side);

        public bool IsWired => playerBar != null && enemyBar != null;

        private void ResolveViewReferences()
        {
            if (playerBar == null)
                playerBar = transform.Find("PlayerArmyBar")?.GetComponent<ArmyHealthBarView>();

            if (enemyBar == null)
                enemyBar = transform.Find("EnemyArmyBar")?.GetComponent<ArmyHealthBarView>();
        }

        private void RefreshBarFractions()
        {
            playerBar?.SetFraction(_tracker.GetFraction(CombatSide.Player));
            enemyBar?.SetFraction(_tracker.GetFraction(CombatSide.Enemy));
        }
    }
}
