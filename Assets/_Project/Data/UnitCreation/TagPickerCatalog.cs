using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Data.UnitCreation
{
    public static class TagPickerCatalog
    {
        public static IReadOnlyList<TagDefinition> PrimaryTags => TagRegistry.GetByCategory(TagCategory.Primary);

        public static IReadOnlyList<TagDefinition> CombatRoleTags => TagRegistry.GetByCategory(TagCategory.CombatRole);

        public static IReadOnlyList<TagDefinition> SystemTags => TagRegistry.GetByCategory(TagCategory.System);

        public static IReadOnlyList<TagDefinition> SynergyTags => TagRegistry.GetByCategory(TagCategory.Synergy);

        public static IReadOnlyList<TagDefinition> AttackTypeTags => TagRegistry.GetByCategory(TagCategory.AttackType);
    }
}
