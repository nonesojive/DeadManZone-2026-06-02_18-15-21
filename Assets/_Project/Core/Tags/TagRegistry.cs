using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public static class TagRegistry
    {
        private static readonly IReadOnlyDictionary<string, TagDefinition> Catalog =
            new Dictionary<string, TagDefinition>(StringComparer.Ordinal)
            {
                // Primary
                [GameTagIds.Infantry] = Create(GameTagIds.Infantry, "Infantry", TagCategory.Primary, "Ground troops that hold front lines.", 100),
                [GameTagIds.Vehicle] = Create(GameTagIds.Vehicle, "Vehicle", TagCategory.Primary, "Armored units with high impact attacks.", 95),
                [GameTagIds.Building] = Create(GameTagIds.Building, "Building", TagCategory.Primary, "Structures that anchor economy or support.", 90),
                [GameTagIds.Structure] = Create(GameTagIds.Structure, "Structure", TagCategory.Primary, "Static fortifications and utility placements.", 85),

                // Combat role
                [GameTagIds.Assault] = Create(GameTagIds.Assault, "Assault", TagCategory.CombatRole, "Aggressive frontline damage dealer.", 80),
                [GameTagIds.Tank] = Create(GameTagIds.Tank, "Tank", TagCategory.CombatRole, "Durable role focused on soaking damage.", 78),
                [GameTagIds.Artillery] = Create(GameTagIds.Artillery, "Artillery", TagCategory.CombatRole, "Long-range fire support specialist.", 76),
                [GameTagIds.Support] = Create(GameTagIds.Support, "Support", TagCategory.CombatRole, "Role that buffs and stabilizes allies.", 74),
                [GameTagIds.Utility] = Create(GameTagIds.Utility, "Utility", TagCategory.CombatRole, "Tactical role focused on control and tools.", 72),
                [GameTagIds.Headquarters] = Create(GameTagIds.Headquarters, "Headquarters", TagCategory.CombatRole, "Command center role for strategic effects.", 70),
                [GameTagIds.Sniper] = Create(GameTagIds.Sniper, "Sniper", TagCategory.CombatRole, "Precision ranged eliminator role.", 68),
                [GameTagIds.Defender] = Create(GameTagIds.Defender, "Defender", TagCategory.CombatRole, "Frontline bulwark role focused on holding ground.", 66),

                // System
                [GameTagIds.Combatant] = Create(GameTagIds.Combatant, "Combatant", TagCategory.System, "System marker for units that can enter combat.", 0, false),
                [GameTagIds.NonCombatant] = Create(GameTagIds.NonCombatant, "Non-Combatant", TagCategory.System, "System marker for units excluded from combat.", 0, false),
                [GameTagIds.Hq] = Create(GameTagIds.Hq, "HQ", TagCategory.System, "System marker for headquarters win condition logic.", 0, false),

                // Attack type
                [GameTagIds.Ballistic] = CreateFromProfile(AttackType.Ballistic),
                [GameTagIds.Piercing] = CreateFromProfile(AttackType.Piercing),
                [GameTagIds.Shredding] = CreateFromProfile(AttackType.Shredding),
                [GameTagIds.Explosive] = CreateFromProfile(AttackType.Explosive),
                [GameTagIds.Fire] = CreateFromProfile(AttackType.Fire),
                [GameTagIds.Melee] = CreateFromProfile(AttackType.Melee),
                [GameTagIds.Gas] = CreateFromProfile(AttackType.Gas)
            };

        public static TagDefinition Get(string id)
        {
            if (TryGet(id, out var tag))
            {
                return tag;
            }

            throw new ArgumentException($"Unknown tag id '{id}'.", nameof(id));
        }

        public static bool TryGet(string id, out TagDefinition tag)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                tag = null;
                return false;
            }

            return Catalog.TryGetValue(id, out tag);
        }

        public static IReadOnlyList<TagDefinition> GetByCategory(TagCategory category) =>
            Catalog.Values
                .Where(t => t.Category == category)
                .OrderByDescending(t => t.DisplayPriority)
                .ToList();

        public static IReadOnlyList<TagDefinition> GetAll() =>
            Catalog.Values
                .OrderByDescending(t => t.DisplayPriority)
                .ToList();

        private static TagDefinition Create(
            string id,
            string displayName,
            TagCategory category,
            string tooltip,
            int displayPriority,
            bool playerVisible = true)
        {
            return new TagDefinition
            {
                Id = id,
                DisplayName = displayName,
                Category = category,
                Tooltip = tooltip,
                DisplayPriority = displayPriority,
                PlayerVisible = playerVisible
            };
        }

        private static TagDefinition CreateFromProfile(AttackType attackType)
        {
            var profile = AttackTypeProfileCatalog.Get(attackType);
            return Create(
                profile.TagId,
                profile.DisplayName,
                TagCategory.AttackType,
                profile.Tooltip,
                displayPriority: 62);
        }
    }
}
