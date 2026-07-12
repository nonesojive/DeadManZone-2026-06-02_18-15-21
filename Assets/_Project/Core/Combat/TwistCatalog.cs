using System;
using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Boss Twists v1 — one authored rule-bend per boss, resolved by id so the
    /// save-restore path rebuilds the exact same modifier. Magnitudes are fixed
    /// for v1 (no per-stage scaling yet).
    /// </summary>
    public static class TwistCatalog
    {
        public const string EndlessMuster = "endless_muster";
        public const string IronDiscipline = "iron_discipline";
        public const string DeathlessCold = "deathless_cold";

        public static ICombatRuleModifier Resolve(string twistId) =>
            twistId switch
            {
                // Militia Warden: the levy never thins — every enemy unit fields +30% HP.
                EndlessMuster => new Twist(EndlessMuster, (_, enemies) =>
                {
                    foreach (var enemy in enemies)
                        enemy.CurrentHp = enemy.CurrentHp * 130 / 100;
                }),

                // Crimson Marshal: drilled to the bone — every enemy unit gains +1
                // permanent armor step (fight-start armor, same channel as synergies).
                IronDiscipline => new Twist(IronDiscipline, (_, enemies) =>
                {
                    foreach (var enemy in enemies)
                        enemy.ArmorBuffSteps += 1;
                }),

                // Wraith Harbinger: the dead hold the line — the enemy front rank
                // (units in the column nearest the player) fields +60% HP.
                DeathlessCold => new Twist(DeathlessCold, (_, enemies) =>
                {
                    var fighters = enemies.Where(e => e.IsAlive && e.Definition.MaxHp > 0).ToList();
                    if (fighters.Count == 0)
                        return;

                    int frontX = fighters.Min(e => e.AnchorPosition.X);
                    foreach (var enemy in fighters.Where(e => e.AnchorPosition.X == frontX))
                        enemy.CurrentHp = enemy.CurrentHp * 160 / 100;
                }),

                _ => throw new InvalidOperationException($"Unknown twist id '{twistId}'.")
            };

        private sealed class Twist : ICombatRuleModifier
        {
            private readonly Action<IList<CombatantState>, IList<CombatantState>> _apply;

            public Twist(string id, Action<IList<CombatantState>, IList<CombatantState>> apply)
            {
                Id = id;
                _apply = apply;
            }

            public string Id { get; }

            public void OnFightStart(
                IList<CombatantState> playerCombatants,
                IList<CombatantState> enemyCombatants) =>
                _apply(playerCombatants, enemyCombatants);
        }
    }
}
