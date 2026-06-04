namespace DeadManZone.Core.Run
{
    public static class EmergencyDraft
    {
        public static bool TryUse(RunState state, int manpowerShortfall)
        {
            if (state == null || state.EmergencyDraftUsed || manpowerShortfall <= 0)
                return false;

            state.Manpower += manpowerShortfall;
            state.EmergencyDraftUsed = true;
            return true;
        }
    }
}
