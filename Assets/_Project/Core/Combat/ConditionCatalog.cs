using System;
using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Battle Conditions v1 — the hard Fight Option's readable rule-bend, drawn seeded
    /// by FightOptionGenerator and shown in the Front Report before the player commits
    /// (consent, not gotcha). Same ICombatRuleModifier seam as boss Twists; the restore
    /// path resolves both through <see cref="RuleModifierCatalog"/>. Flavors are
    /// deliberately distinct from the twist deck (all-enemy +HP / all-enemy +armor /
    /// front-rank +HP). Magnitudes are M2 initial, tune in playtest.
    /// </summary>
    public static class ConditionCatalog
    {
        public const string EntrenchedFoe = "entrenched_foe";
        public const string VeteranCadre = "veteran_cadre";
        public const string StormBarrage = "storm_barrage";
        public const string IronResolve = "iron_resolve";

        /// <summary>Generator draw order. APPEND-ONLY: ids are seeded rolls persisted
        /// in saves, so reordering or removing entries breaks determinism.</summary>
        public static readonly IReadOnlyList<string> Ids = new[]
        {
            EntrenchedFoe, VeteranCadre, StormBarrage, IronResolve
        };

        /// <summary>
        /// Player-facing name for the Front Report chip.
        /// </summary>
        public static string DisplayName(string conditionId) => conditionId switch
        {
            EntrenchedFoe => "ENTRENCHED FOE",
            VeteranCadre => "VETERAN CADRE",
            StormBarrage => "STORM BARRAGE",
            IronResolve => "IRON RESOLVE",
            _ => string.IsNullOrEmpty(conditionId)
                ? string.Empty
                : conditionId.Replace('_', ' ').ToUpperInvariant()
        };

        /// <summary>
        /// What the condition actually DOES, in the player's words. Consent, not gotcha — the
        /// Front Report shows this before the fight is taken.
        ///
        /// Deliberately kept in this file, next to the behaviour it describes: the magnitudes
        /// below are duplicated from the modifiers a few lines down, and separating them is how
        /// tooltip text silently drifts away from the rules it claims to explain. Change one,
        /// change the other.
        /// </summary>
        public static string Describe(string conditionId) => conditionId switch
        {
            EntrenchedFoe => "Dug in for weeks. The enemy front rank — the column nearest you — holds with +1 armor step.",
            VeteranCadre => "Hardened survivors man the guns. Every enemy BEHIND the front rank fields +25% HP.",
            StormBarrage => "Your march in was through an artillery storm. Every one of your units starts the fight at -15% HP.",
            IronResolve => "Zealots to the last. Every enemy unit hits +1 harder.",
            _ => string.Empty
        };

        public static bool TryResolve(string conditionId, out ICombatRuleModifier modifier)
        {
            modifier = conditionId switch
            {
                // Dug in for weeks: the enemy front rank (column nearest the player)
                // holds with +1 permanent armor step.
                EntrenchedFoe => new Condition(EntrenchedFoe, (_, enemies) =>
                {
                    var fighters = Fighters(enemies);
                    if (fighters.Count == 0)
                        return;

                    int frontX = fighters.Min(e => e.AnchorPosition.X);
                    foreach (var enemy in fighters.Where(e => e.AnchorPosition.X == frontX))
                        enemy.ArmorBuffSteps += 1;
                }),

                // Hardened survivors man the guns: every enemy BEHIND the front rank
                // (support/rear columns) fields +25% HP.
                VeteranCadre => new Condition(VeteranCadre, (_, enemies) =>
                {
                    var fighters = Fighters(enemies);
                    if (fighters.Count == 0)
                        return;

                    int frontX = fighters.Min(e => e.AnchorPosition.X);
                    foreach (var enemy in fighters.Where(e => e.AnchorPosition.X != frontX))
                        enemy.CurrentHp = enemy.CurrentHp * 125 / 100;
                }),

                // The march in was through an artillery storm: every player unit
                // starts the fight at -15% HP (never below 1).
                StormBarrage => new Condition(StormBarrage, (players, _) =>
                {
                    foreach (var unit in Fighters(players))
                        unit.CurrentHp = Math.Max(1, unit.CurrentHp * 85 / 100);
                }),

                // Zealots to the last: every enemy unit hits +1 harder.
                IronResolve => new Condition(IronResolve, (_, enemies) =>
                {
                    foreach (var enemy in enemies)
                        enemy.DamageBonus += 1;
                }),

                _ => null
            };
            return modifier != null;
        }

        private static List<CombatantState> Fighters(IList<CombatantState> combatants) =>
            combatants.Where(c => c.IsAlive && c.Definition.MaxHp > 0).ToList();

        private sealed class Condition : ICombatRuleModifier
        {
            private readonly Action<IList<CombatantState>, IList<CombatantState>> _apply;

            public Condition(string id, Action<IList<CombatantState>, IList<CombatantState>> apply)
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

    /// <summary>Single resolve entry point for every rule-modifier id a save can carry —
    /// boss Twists and Battle Conditions share CombatSaveState.ActiveTwistId.</summary>
    public static class RuleModifierCatalog
    {
        public static ICombatRuleModifier Resolve(string id) =>
            ConditionCatalog.TryResolve(id, out var condition)
                ? condition
                : TwistCatalog.Resolve(id);
    }
}
