using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public static class AttackTypeTags
    {
        public static string ToTagId(AttackType attackType)
        {
            if (attackType == AttackType.None)
                return null;

            return attackType.ToString().ToLowerInvariant();
        }

        public static bool TryFromTagId(string tagId, out AttackType attackType)
        {
            attackType = AttackType.None;
            if (string.IsNullOrWhiteSpace(tagId))
                return false;

            foreach (AttackType value in System.Enum.GetValues(typeof(AttackType)))
            {
                if (value == AttackType.None)
                    continue;

                if (string.Equals(ToTagId(value), tagId.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    attackType = value;
                    return true;
                }
            }

            return false;
        }
    }
}
