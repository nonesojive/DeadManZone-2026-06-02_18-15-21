using System.Collections.Generic;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// One readable rule-bend applied to a fight at start, after the standard
    /// fight-start engines (synergy, critical mass, tactic buffs) have run.
    /// Shared seam for Boss Fight Twists (M1) and Battle Conditions (M2) —
    /// resolved by id on the save-restore path, so implementations must be
    /// deterministic and stateless.
    /// </summary>
    public interface ICombatRuleModifier
    {
        string Id { get; }

        void OnFightStart(
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants);
    }
}
