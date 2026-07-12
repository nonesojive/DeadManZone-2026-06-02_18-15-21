using DeadManZone.Core;
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class EmergencyDraftTests
    {
        [Test]
        public void TryUse_AddsShortfallAndMarksUsed()
        {
            var state = RunState.CreateNew(FactionIds.IronmarchUnion, 1, 10, 2, 5);

            Assert.IsTrue(EmergencyDraft.TryUse(state, manpowerShortfall: 3));
            Assert.AreEqual(5, state.Manpower);
            Assert.IsTrue(state.EmergencyDraftUsed);
        }

        [Test]
        public void TryUse_ReturnsFalseWhenAlreadyUsed()
        {
            var state = RunState.CreateNew(FactionIds.IronmarchUnion, 1, 10, 5, 5);
            state.EmergencyDraftUsed = true;

            Assert.IsFalse(EmergencyDraft.TryUse(state, manpowerShortfall: 2));
            Assert.AreEqual(5, state.Manpower);
        }

        [Test]
        public void TryUse_ReturnsFalseForNonPositiveShortfall()
        {
            var state = RunState.CreateNew(FactionIds.IronmarchUnion, 1, 10, 5, 5);

            Assert.IsFalse(EmergencyDraft.TryUse(state, manpowerShortfall: 0));
            Assert.IsFalse(state.EmergencyDraftUsed);
        }
    }
}
