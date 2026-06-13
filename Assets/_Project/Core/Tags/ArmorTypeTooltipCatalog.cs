using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public static class ArmorTypeTooltipCatalog
    {
        public static string GetTooltip(ArmorType armorType) =>
            armorType switch
            {
                ArmorType.Light => "Light protection; weak vs Shredding, Fire, and Melee.",
                ArmorType.Medium => "Standard armor; weak vs Ballistic, strong vs Shredding.",
                ArmorType.Heavy => "Heavy plating; weak vs Piercing and Explosive.",
                _ => string.Empty
            };
    }
}
