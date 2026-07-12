using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    /// <summary>Army-wide HP totals for Combatant-tagged units (HQ/buildings excluded).</summary>
    public readonly struct ArmyHealth
    {
        public int CurrentHp { get; init; }
        public int StartingHp { get; init; }
        public float Fraction => StartingHp <= 0 ? 0f : (float)CurrentHp / StartingHp;
    }

    public static class ArmyHealthTracker
    {
        public static ArmyHealth Evaluate(IEnumerable<CombatantState> combatants)
        {
            int current = 0;
            int starting = 0;
            if (combatants != null)
            {
                foreach (var combatant in combatants)
                {
                    if (!PieceCombatRules.ParticipatesInCombat(combatant.Definition))
                        continue;

                    starting += combatant.Definition.MaxHp;
                    // Routed units read as dead on the army bar: 0 current, full weight in max.
                    current += combatant.IsBroken ? 0 : System.Math.Max(0, combatant.CurrentHp);
                }
            }

            return new ArmyHealth { CurrentHp = current, StartingHp = starting };
        }
    }
}
