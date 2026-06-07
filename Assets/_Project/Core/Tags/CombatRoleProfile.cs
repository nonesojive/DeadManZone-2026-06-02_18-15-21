using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public enum CombatRoleTargetingBias
    {
        DefaultFirstByInstanceId,
        HighestHp,
        Furthest,
        NearestFront,
        LowestMaxHpRearPreferred,
        NoAttack
    }

    public static class CombatRoleProfile
    {
        private static readonly IReadOnlyDictionary<string, CombatRoleTargetingBias> Catalog =
            new Dictionary<string, CombatRoleTargetingBias>(StringComparer.OrdinalIgnoreCase)
            {
                [GameTagIds.Assault] = CombatRoleTargetingBias.HighestHp,
                [GameTagIds.Tank] = CombatRoleTargetingBias.NearestFront,
                [GameTagIds.Artillery] = CombatRoleTargetingBias.Furthest,
                [GameTagIds.Support] = CombatRoleTargetingBias.LowestMaxHpRearPreferred,
                [GameTagIds.Sniper] = CombatRoleTargetingBias.HighestHp,
                [GameTagIds.Utility] = CombatRoleTargetingBias.NoAttack,
                [GameTagIds.Headquarters] = CombatRoleTargetingBias.NoAttack
            };

        public static CombatRoleTargetingBias ResolveBias(string combatRole)
        {
            if (string.IsNullOrWhiteSpace(combatRole))
                return CombatRoleTargetingBias.DefaultFirstByInstanceId;

            return Catalog.TryGetValue(combatRole.Trim(), out var bias)
                ? bias
                : CombatRoleTargetingBias.DefaultFirstByInstanceId;
        }
    }
}
