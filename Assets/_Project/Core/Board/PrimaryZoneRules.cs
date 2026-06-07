using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    public static class PrimaryZoneRules
    {
        public static bool IsZoneAllowed(string primaryTag, ZoneType zoneType)
        {
            if (string.IsNullOrWhiteSpace(primaryTag))
                return true;

            string normalizedPrimary = primaryTag.Trim().ToLowerInvariant();
            return normalizedPrimary switch
            {
                GameTagIds.Building => zoneType == ZoneType.Rear,
                GameTagIds.Infantry or GameTagIds.Vehicle => zoneType is ZoneType.Front or ZoneType.Support,
                GameTagIds.Structure => zoneType is ZoneType.Rear or ZoneType.Support or ZoneType.Front,
                _ => false
            };
        }
    }
}
