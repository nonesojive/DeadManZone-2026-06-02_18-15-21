using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using DeadManZone.Data.UnitCreation;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class UnitCreatorFormSections
    {
        public static void DrawIdentity(UnitCreationDraft draft, ContentDatabase database)
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            draft.id = EditorGUILayout.TextField("Id", draft.id);
            draft.displayName = EditorGUILayout.TextField("Display Name", draft.displayName);
            draft.category = (PieceCategory)EditorGUILayout.EnumPopup("Category", draft.category);

            DrawFactionDropdown(draft, database);
            EditorGUILayout.LabelField("Asset Path", UnitPersistenceService.GetPieceAssetPath(draft.id ?? string.Empty));
        }

        public static void DrawTags(UnitCreationDraft draft)
        {
            EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
            draft.primary = DrawTagPopup("Primary", draft.primary, TagRegistry.GetByCategory(TagCategory.Primary));
            draft.combatRole = DrawTagPopup(
                "Combat Role",
                draft.combatRole,
                TagRegistry.GetByCategory(TagCategory.CombatRole),
                allowEmpty: true);
            DrawCategoryChecklist("Synergy Tags", draft.synergyTags, TagRegistry.GetByCategory(TagCategory.Synergy));
            DrawCategoryChecklist("Ability Tags", draft.abilityTags, TagRegistry.GetByCategory(TagCategory.Ability));
            DrawCategoryChecklist("Flavor Tags", draft.flavorTags, TagRegistry.GetByCategory(TagCategory.Flavor));
        }

        public static void DrawStats(UnitCreationDraft draft)
        {
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            draft.maxHp = EditorGUILayout.IntField("Max HP", draft.maxHp);
            draft.baseDamage = EditorGUILayout.IntField("Base Damage", draft.baseDamage);
            draft.cooldownTicks = EditorGUILayout.IntField("Cooldown Ticks", draft.cooldownTicks);
            draft.goldCost = EditorGUILayout.IntField("Gold Cost", draft.goldCost);
            draft.requisitionCost = EditorGUILayout.IntField("Requisition Cost", draft.requisitionCost);
            draft.manpowerCost = EditorGUILayout.IntField("Manpower Cost", draft.manpowerCost);
            draft.musterPerShop = EditorGUILayout.IntField("Muster Per Shop", draft.musterPerShop);
            draft.attackSpeed = (AttackSpeedTier)EditorGUILayout.EnumPopup("Attack Speed", draft.attackSpeed);
            draft.attackRange = (AttackRangeTier)EditorGUILayout.EnumPopup("Attack Range", draft.attackRange);
            draft.movementSpeed = (MovementSpeedTier)EditorGUILayout.EnumPopup("Movement Speed", draft.movementSpeed);
            draft.armorType = (ArmorType)EditorGUILayout.EnumPopup("Armor Type", draft.armorType);
            draft.attackType = (AttackType)EditorGUILayout.EnumPopup("Attack Type", draft.attackType);
            draft.grantedAbility = (GrantedAbility)EditorGUILayout.EnumPopup("Granted Ability", draft.grantedAbility);
            draft.accuracyOverride = EditorGUILayout.IntSlider(
                new GUIContent("Accuracy Override", "0 = use attack type / role defaults. Otherwise 1–100."),
                draft.accuracyOverride,
                0,
                100);
            draft.shopModifiers = (ShopModifierFlags)EditorGUILayout.EnumFlagsField("Shop Modifiers", draft.shopModifiers);
            draft.commandActions = (CommandActionFlags)EditorGUILayout.EnumFlagsField("Command Actions", draft.commandActions);
        }

        public static void DrawVisuals(UnitCreationDraft draft)
        {
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            draft.icon = (Sprite)EditorGUILayout.ObjectField("Icon", draft.icon, typeof(Sprite), false);
            draft.categoryTint = EditorGUILayout.ColorField("Category Tint", draft.categoryTint);

            if (GUILayout.Button("Auto-Assign Cell Sprites From Renders"))
                UnitCreatorArtUtility.TryAutoAssignCellSprites(draft);
        }

        public static void DrawRegistration(UnitCreationDraft draft)
        {
            EditorGUILayout.LabelField("Registration", EditorStyles.boldLabel);
            draft.addToContentDatabase = EditorGUILayout.Toggle("Add to ContentDatabase", draft.addToContentDatabase);
            draft.includeInShopPool = EditorGUILayout.Toggle("Include in Shop Pool", draft.includeInShopPool);

            var laneDetail = draft.ComputedShopLaneDetail;
            var laneLabel = laneDetail.Lane.ToString();
            if (laneDetail.Confidence == ShopLaneResolveConfidence.SpecialtyPendingRules)
                laneLabel += " (rules pending)";
            EditorGUILayout.LabelField("Computed Shop Lane", laneLabel);
        }

        public static void DrawValidation(UnitCreationValidationResult result)
        {
            if (result == null || result.Messages.Count == 0)
                return;

            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            foreach (var message in result.Messages)
            {
                var messageType = message.Severity switch
                {
                    ValidationSeverity.Error => MessageType.Error,
                    ValidationSeverity.Warning => MessageType.Warning,
                    _ => MessageType.Info
                };
                EditorGUILayout.HelpBox(message.Message, messageType);
            }
        }

        private static void DrawFactionDropdown(UnitCreationDraft draft, ContentDatabase database)
        {
            var factions = database?.Factions?.Where(f => f != null).ToArray() ?? System.Array.Empty<FactionSO>();
            if (factions.Length == 0)
            {
                draft.factionId = EditorGUILayout.TextField("Faction Id", draft.factionId);
                return;
            }

            var labels = factions.Select(f => $"{f.displayName} ({f.factionId})").ToArray();
            var ids = factions.Select(f => f.factionId).ToArray();
            int index = System.Array.IndexOf(ids, draft.factionId);
            if (index < 0)
                index = 0;

            index = EditorGUILayout.Popup("Faction", index, labels);
            draft.factionId = ids[index];
        }

        private static string DrawTagPopup(
            string label,
            string current,
            System.Collections.Generic.IReadOnlyList<TagDefinition> options,
            bool allowEmpty = false)
        {
            var ids = options.Select(o => o.Id).ToList();
            var labels = options.Select(o => o.DisplayName).ToList();
            if (allowEmpty)
            {
                ids.Insert(0, string.Empty);
                labels.Insert(0, "(None)");
            }

            int index = ids.IndexOf(current ?? string.Empty);
            if (index < 0)
                index = 0;

            index = EditorGUILayout.Popup(label, index, labels.ToArray());
            return ids[index];
        }

        private static void DrawCategoryChecklist(
            string label,
            System.Collections.Generic.List<string> selectedIds,
            System.Collections.Generic.IReadOnlyList<TagDefinition> options)
        {
            EditorGUILayout.LabelField(label);
            foreach (var tag in options)
            {
                bool selected = selectedIds.Contains(tag.Id);
                bool next = EditorGUILayout.ToggleLeft(tag.DisplayName, selected);
                if (next && !selected)
                    selectedIds.Add(tag.Id);
                else if (!next && selected)
                    selectedIds.Remove(tag.Id);
            }
        }
    }
}
