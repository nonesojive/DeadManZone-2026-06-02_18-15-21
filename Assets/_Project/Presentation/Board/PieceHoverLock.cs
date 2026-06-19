using System;

namespace DeadManZone.Presentation.Board
{
    public sealed class PieceHoverLock
    {
        private string _activeInstanceId;
        private int _depth;

        public bool HasActiveHover => _depth > 0;

        public void Enter(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            if (_activeInstanceId == instanceId)
            {
                _depth++;
                return;
            }

            _activeInstanceId = instanceId;
            _depth = 1;
        }

        public void Exit(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) || _activeInstanceId != instanceId)
                return;

            _depth = Math.Max(0, _depth - 1);
            if (_depth == 0)
                _activeInstanceId = null;
        }

        public bool ShouldShow(string instanceId) =>
            !string.IsNullOrEmpty(instanceId) && _activeInstanceId == instanceId && _depth > 0;

        public void Clear()
        {
            _activeInstanceId = null;
            _depth = 0;
        }
    }
}
