using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public readonly struct KeywordTagEntry
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public TagCategory Category { get; init; }
        public string Tooltip { get; init; }
        public int DisplayPriority { get; init; }
    }

    /// <summary>
    /// Synergy, ability, and flavor tags from the design sheet.
    /// </summary>
    public static class KeywordTagCatalog
    {
        private static readonly KeywordTagEntry[] Entries =
        {
            // Synergy
            Entry(GameTagIds.Phalanx, "Phalanx", TagCategory.Synergy, "Gains +1 damage and +10 HP for each adjacent infantry.", 60),
            Entry(GameTagIds.Inspiring, "Inspiring", TagCategory.Synergy, "Adjacent units get +1 Move.", 58),
            Entry(GameTagIds.Medic, "Medic", TagCategory.Synergy, "Adjacent infantry get +10 HP.", 56),
            Entry(GameTagIds.Mechanic, "Mechanic", TagCategory.Synergy, "Gives adjacent vehicles the Repair tag.", 54),
            Entry(GameTagIds.Spotter, "Spotter", TagCategory.Synergy, "Adjacent snipers get +1 range.", 52),
            Entry(GameTagIds.Fortify, "Fortify", TagCategory.Synergy, "Adjacent units get +1 armor.", 50),
            Entry(GameTagIds.Jammer, "Jammer", TagCategory.Synergy, "Stealth and Ambush of adjacent units trigger twice.", 48),
            Entry(GameTagIds.Bunker, "Bunker", TagCategory.Synergy, "Adjacent infantry get +1 armor.", 46),
            Entry(GameTagIds.Fanatic, "Fanatic", TagCategory.Synergy, "Gains +1 damage for each adjacent Fanatic.", 44),
            Entry(GameTagIds.Supplier, "Supplier", TagCategory.Synergy, "Doubles adjacent Logistics.", 42),
            Entry(GameTagIds.Entrenched, "Entrenched", TagCategory.Synergy, "Gains +1 armor for each adjacent Fortification.", 40),
            Entry(GameTagIds.Bombard, "Bombard", TagCategory.Synergy, "Grants Bombard Tactic.", 38),
            Entry(GameTagIds.GasCloud, "Gas Cloud", TagCategory.Synergy, "Gain Gas Cloud attack.", 36),
            Entry(GameTagIds.Convoy, "Convoy", TagCategory.Synergy, "Logistics convoy support.", 34),
            Entry(GameTagIds.SupplyLine, "Supply Line", TagCategory.Synergy, "Extended supply network.", 32),
            Entry(GameTagIds.GasDivision, "Gas Division", TagCategory.Synergy, "Coordinated gas warfare unit.", 30),
            Entry(GameTagIds.ChemicalCorps, "Chemical Corps", TagCategory.Synergy, "Specialized chemical warfare corps.", 28),

            // Ability
            Entry(GameTagIds.Stealth, "Stealth", TagCategory.Ability, "Remains hidden until this attacks.", 55),
            Entry(GameTagIds.Ambush, "Ambush", TagCategory.Ability, "First attack each phase does x2 damage.", 53),
            Entry(GameTagIds.Berserk, "Berserk", TagCategory.Ability, "When below 25% HP gains x2 damage and +1 move.", 51),
            Entry(GameTagIds.Emp, "EMP", TagCategory.Ability, "Temporarily disables vehicles.", 49),
            Entry(GameTagIds.Grenadier, "Grenadier", TagCategory.Ability, "Throws explosive grenade.", 47),
            Entry(GameTagIds.Suppression, "Suppression", TagCategory.Ability, "Increases cooldown of enemies.", 45),
            Entry(GameTagIds.Repair, "Repair", TagCategory.Ability, "Heals HP periodically.", 43),
            Entry(GameTagIds.LastStand, "Last Stand", TagCategory.Ability, "The first time HP reaches 0, gain 25% max HP.", 41),
            Entry(GameTagIds.Taunt, "Taunt", TagCategory.Ability, "Enemies prioritize attacking this piece.", 39),
            Entry(GameTagIds.Flamethrower, "Flamethrower", TagCategory.Ability, "Gain flamethrower attack.", 37),
            Entry(GameTagIds.Echo, "Echo", TagCategory.Ability, "Doubles buffs given to this unit.", 35),
            Entry(GameTagIds.Toxic, "Toxic", TagCategory.Ability, "Applies toxic damage over time.", 33),
            Entry(GameTagIds.Ironclad, "Ironclad", TagCategory.Ability, "Extreme damage resistance under fire.", 31),

            // Flavor
            Entry(GameTagIds.Fortified, "Fortified", TagCategory.Flavor, "This unit has +1 armor.", 50),
            Entry(GameTagIds.Veteran, "Veteran", TagCategory.Flavor, "Seasoned unit with combat experience.", 48),
            Entry(GameTagIds.Prototype, "Prototype", TagCategory.Flavor, "Experimental design.", 46),
            Entry(GameTagIds.Mercenary, "Mercenary", TagCategory.Flavor, "Hired gun.", 44),
            Entry(GameTagIds.Siege, "Siege", TagCategory.Flavor, "Siege specialist.", 42),
            Entry(GameTagIds.Fortification, "Fortification", TagCategory.Flavor, "Static defensive emplacement.", 40),
            Entry(GameTagIds.Logistics, "Logistics", TagCategory.Flavor, "Gives +10 Supplies.", 38),
            Entry(GameTagIds.Command, "Command", TagCategory.Flavor, "Gives +1 Authority.", 36),
            Entry(GameTagIds.Bomber, "Bomber", TagCategory.Flavor, "Bomber specialization.", 34),
            Entry(GameTagIds.Airstrip, "Airstrip", TagCategory.Flavor, "Airfield support.", 32),
            Entry(GameTagIds.GasMask, "Gas Mask", TagCategory.Flavor, "Resistance to Gas.", 30),
            Entry(GameTagIds.Bastion, "Bastion", TagCategory.Flavor, "Defensive bulwark.", 28)
        };

        public static IReadOnlyList<KeywordTagEntry> All { get; } = Entries;

        public static IReadOnlyList<KeywordTagEntry> GetByCategory(TagCategory category)
        {
            var matches = new List<KeywordTagEntry>();
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].Category == category)
                    matches.Add(Entries[i]);
            }

            return matches;
        }

        private static KeywordTagEntry Entry(
            string id,
            string displayName,
            TagCategory category,
            string tooltip,
            int displayPriority)
        {
            return new KeywordTagEntry
            {
                Id = id,
                DisplayName = displayName,
                Category = category,
                Tooltip = tooltip,
                DisplayPriority = displayPriority
            };
        }
    }
}
