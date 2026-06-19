using DeadManZone.Presentation.Board;
using NUnit.Framework;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class PieceHoverLockTests
    {
        [Test]
        public void Enter_IncrementsDepthForSameInstance()
        {
            var lockState = new PieceHoverLock();
            lockState.Enter("a");
            lockState.Enter("a");
            Assert.IsTrue(lockState.ShouldShow("a"));
            lockState.Exit("a");
            Assert.IsTrue(lockState.ShouldShow("a"));
            lockState.Exit("a");
            Assert.IsFalse(lockState.ShouldShow("a"));
        }

        [Test]
        public void Exit_WrongInstance_DoesNotClearActive()
        {
            var lockState = new PieceHoverLock();
            lockState.Enter("a");
            lockState.Exit("b");
            Assert.IsTrue(lockState.ShouldShow("a"));
        }
    }
}
