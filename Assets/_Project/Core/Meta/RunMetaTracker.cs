using System;
using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Meta
{
    public sealed class RunMetaTracker
    {
        public int SalvageSuppliesThisRun { get; private set; }
        public int CriticalMassTriggersThisRun { get; private set; }
        public bool HqDamagedThisFight { get; private set; }
        public int FightsCompletedThisRun { get; private set; }

        public void RecordSalvage(int supplies) => SalvageSuppliesThisRun += supplies;

        public void RecordCriticalMassTrigger() => CriticalMassTriggersThisRun++;

        public void RecordHqDamaged() => HqDamagedThisFight = true;

        public void RecordFightCompleted() => FightsCompletedThisRun++;

        public void ResetFightFlags() => HqDamagedThisFight = false;

        public void ResetRun()
        {
            SalvageSuppliesThisRun = 0;
            CriticalMassTriggersThisRun = 0;
            HqDamagedThisFight = false;
            FightsCompletedThisRun = 0;
        }

        public IEnumerable<string> EvaluateRunEndAchievements(
            bool victory,
            string factionId,
            int finalMorale)
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

            if (finalMorale >= 100)
                yield return AchievementIds.PerfectMorale;

            switch (factionId)
            {
                case "iron_vanguard":
                    yield return AchievementIds.WinIronmarch;
                    break;
                case "dust_scourge":
                    yield return AchievementIds.WinDustScourge;
                    break;
                case "cartel_of_echoes":
                    yield return AchievementIds.WinCartel;
                    break;
            }
        }

        public IEnumerable<string> EvaluateFightEndAchievements(bool playerWon)
        {
            if (playerWon && !HqDamagedThisFight)
                yield return AchievementIds.WinNoHqDamage;
        }
    }
}
