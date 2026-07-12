using System.Collections.Generic;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Rebuilds army HP fractions from replayed combat events so UI bars stay in
    /// sync with what the player is watching (the sim itself finishes instantly).
    /// </summary>
    public sealed class ArmyHealthReplayTracker
    {
        private sealed class UnitHealth
        {
            public CombatSide Side;
            public int MaxHp;
            public int CurrentHp;
        }

        private readonly Dictionary<string, UnitHealth> _units = new();

        public void Clear() => _units.Clear();

        public void RegisterUnit(string instanceId, CombatSide side, int maxHp)
        {
            _units[instanceId] = new UnitHealth { Side = side, MaxHp = maxHp, CurrentHp = maxHp };
        }

        public void ApplyEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null)
                return;

            switch (combatEvent.ActionType)
            {
                // All HP-reducing actions target TargetId with Value damage.
                case "damage":
                case "graze":
                case "gas_damage":
                case "mortar_shot":
                case "cannon_blast":
                case "cannon_blast_splash":
                case "call_strike":
                    if (_units.TryGetValue(combatEvent.TargetId ?? string.Empty, out var hit))
                        hit.CurrentHp = System.Math.Max(0, hit.CurrentHp - combatEvent.Value);
                    break;

                // "destroyed" events carry the victim in ActorId.
                case "destroyed":
                    if (_units.TryGetValue(combatEvent.ActorId ?? string.Empty, out var dead))
                        dead.CurrentHp = 0;
                    break;
            }
        }

        /// <summary>Current HP fraction for a single unit; false when unregistered.</summary>
        public bool TryGetUnitFraction(string instanceId, out float fraction)
        {
            fraction = 0f;
            if (string.IsNullOrEmpty(instanceId) || !_units.TryGetValue(instanceId, out var unit))
                return false;

            fraction = unit.MaxHp <= 0 ? 0f : (float)unit.CurrentHp / unit.MaxHp;
            return true;
        }

        public float GetFraction(CombatSide side)
        {
            int current = 0;
            int max = 0;
            foreach (var unit in _units.Values)
            {
                if (unit.Side != side)
                    continue;

                current += unit.CurrentHp;
                max += unit.MaxHp;
            }

            return max <= 0 ? 0f : (float)current / max;
        }
    }
}
