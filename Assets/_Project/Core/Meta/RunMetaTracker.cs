using System.Collections.Generic;
using DeadManZone.Core;

namespace DeadManZone.Core.Meta
{
    public sealed class RunMetaTracker
    {
        public int SalvageSuppliesThisRun { get; private set; }
        public int CriticalMassTriggersThisRun { get; private set; }
        public int FightsCompletedThisRun { get; private set; }

        public void RecordSalvage(int supplies) => SalvageSuppliesThisRun += supplies;

        public void RecordCriticalMassTrigger() => CriticalMassTriggersThisRun++;

        public void RecordFightCompleted() => FightsCompletedThisRun++;

        public void ResetRun()
        {
            SalvageSuppliesThisRun = 0;
            CriticalMassTriggersThisRun = 0;
            FightsCompletedThisRun = 0;
        }

        public IEnumerable<string> EvaluateRunEndAchievements(
            bool victory,
            string factionId,
            int finalManpower)
        {
            if (FightsCompletedThisRun >= 10)
                yield return AchievementIds.TenFightsSurvived;

            if (SalvageSuppliesThisRun >= 100)
                yield return AchievementIds.SalvageHundred;

            if (CriticalMassTriggersThisRun >= 5)
                yield return AchievementIds.CriticalMassFive;

            if (!victory)
                yield break;

            yield return AchievementIds.ClearGauntlet;

            // Run-level Morale retired (ADR-0005, M5): the "unbroken" bar is now the
            // army itself — win with 100+ Manpower standing. Id stays for Steam parity.
            if (finalManpower >= 100)
                yield return AchievementIds.PerfectMorale;

            switch (factionId)
            {
                case FactionIds.IronmarchUnion:
                    yield return AchievementIds.WinIronmarch;
                    break;
                case FactionIds.DustScourge:
                    yield return AchievementIds.WinDustScourge;
                    break;
                case FactionIds.CartelOfEchoes:
                    yield return AchievementIds.WinCartel;
                    break;
            }
        }
    }
}
