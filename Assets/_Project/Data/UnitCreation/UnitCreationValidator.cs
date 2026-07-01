using System.Collections.Generic;
using System.Text.RegularExpressions;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;

namespace DeadManZone.Data.UnitCreation
{
    public enum ValidationSeverity
    {
        Error,
        Warning,
        Info
    }

    public readonly struct ValidationMessage
    {
        public ValidationMessage(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public ValidationSeverity Severity { get; }
        public string Message { get; }
    }

    public sealed class UnitCreationValidationResult
    {
        private readonly List<ValidationMessage> _messages = new();

        public IReadOnlyList<ValidationMessage> Messages => _messages;
        public bool HasErrors => _messages.Exists(m => m.Severity == ValidationSeverity.Error);

        public void Add(ValidationSeverity severity, string message) =>
            _messages.Add(new ValidationMessage(severity, message));
    }

    public static class UnitCreationValidator
    {
        private static readonly Regex IdPattern = new("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

        public static UnitCreationValidationResult Validate(
            UnitCreationDraft draft,
            bool idExistsInProject,
            bool idRegisteredInDatabase)
        {
            var result = new UnitCreationValidationResult();
            if (draft == null)
            {
                result.Add(ValidationSeverity.Error, "Draft is null.");
                return result;
            }

            ValidateIdentity(draft, idExistsInProject, result);
            ValidateTags(draft, result);
            ValidateShape(draft, result);
            ValidateShopLane(draft, result);

            if (draft.addToContentDatabase && idRegisteredInDatabase && draft.Mode == UnitCreatorMode.Create)
                result.Add(ValidationSeverity.Info, "Piece id is already registered in ContentDatabase; save will update the existing entry.");

            return result;
        }

        private static void ValidateIdentity(UnitCreationDraft draft, bool idExistsInProject, UnitCreationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(draft.id))
            {
                result.Add(ValidationSeverity.Error, "Unit id is required.");
                return;
            }

            if (!IdPattern.IsMatch(draft.id.Trim()))
                result.Add(ValidationSeverity.Error, "Unit id must be snake_case (lowercase letters, numbers, underscores; start with a letter).");

            if (draft.Mode == UnitCreatorMode.Create && idExistsInProject)
                result.Add(ValidationSeverity.Error, $"A piece asset already exists for id '{draft.id}'.");

            if (string.IsNullOrWhiteSpace(draft.displayName))
                result.Add(ValidationSeverity.Error, "Display name is required.");
        }

        private static void ValidateTags(UnitCreationDraft draft, UnitCreationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(draft.primary))
            {
                result.Add(ValidationSeverity.Error, "Primary tag is required.");
            }
            else if (!TagRegistry.TryGet(draft.primary, out _))
            {
                result.Add(ValidationSeverity.Warning, $"Unknown primary tag '{draft.primary}'.");
            }

            WarnIfUnknown(draft.combatRole, "combat role", result);
            WarnTagList(draft.synergyTags, "synergy tag", result);
            WarnTagList(draft.abilityTags, "ability tag", result);
            WarnTagList(draft.flavorTags, "flavor tag", result);
        }

        private static void ValidateShape(UnitCreationDraft draft, UnitCreationValidationResult result)
        {
            if (draft.shapeCells == null || draft.shapeCells.Length == 0)
                result.Add(ValidationSeverity.Error, "Shape must include at least one cell.");
        }

        private static void ValidateShopLane(UnitCreationDraft draft, UnitCreationValidationResult result)
        {
            var resolved = draft.ComputedShopLaneDetail;
            switch (resolved.Confidence)
            {
                case ShopLaneResolveConfidence.SpecialtyPendingRules:
                    result.Add(ValidationSeverity.Info,
                        "Specialty lane placement rules are not finalized — verify this unit belongs in the Requisition lane.");
                    break;
                case ShopLaneResolveConfidence.UnknownRoleFallback:
                    result.Add(ValidationSeverity.Warning,
                        string.IsNullOrWhiteSpace(draft.combatRole)
                            ? "No combat role selected; defaulting shop lane to Offensive."
                            : $"Unknown combat role '{draft.combatRole}'; defaulting shop lane to Offensive.");
                    break;
            }
        }

        private static void WarnIfUnknown(string tagId, string label, UnitCreationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                return;

            if (!TagRegistry.TryGet(tagId, out _))
                result.Add(ValidationSeverity.Warning, $"Unknown {label} '{tagId}'.");
        }

        private static void WarnTagList(IEnumerable<string> tags, string label, UnitCreationValidationResult result)
        {
            if (tags == null)
                return;

            foreach (var tagId in tags)
            {
                if (string.IsNullOrWhiteSpace(tagId))
                    continue;

                if (!TagRegistry.TryGet(tagId, out _))
                    result.Add(ValidationSeverity.Warning, $"Unknown {label} '{tagId}'.");
            }
        }
    }
}
