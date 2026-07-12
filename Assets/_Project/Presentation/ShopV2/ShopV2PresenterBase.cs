using DeadManZone.Core.Run;
using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Base for ShopV2 presenters: binds Refresh to RunManager.RunStateChanged, retrying until the singleton exists.</summary>
    public abstract class ShopV2PresenterBase : MonoBehaviour
    {
        private bool _subscribed;

        protected virtual void OnEnable() => TrySubscribe();

        protected virtual void OnDisable()
        {
            if (_subscribed && RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
            _subscribed = false;
        }

        private void Update()
        {
            // Defensive: RunManager may spawn after this canvas. Retry until found, then this is a no-op bool check.
            if (!_subscribed)
                TrySubscribe();
        }

        private void TrySubscribe()
        {
            var manager = RunManager.Instance;
            if (manager == null)
                return;

            manager.RunStateChanged += OnRunStateChanged;
            _subscribed = true;

            if (manager.HasActiveRun)
                Refresh(manager.State);
        }

        private void OnRunStateChanged(RunState state)
        {
            if (state != null)
                Refresh(state);
        }

        protected abstract void Refresh(RunState state);
    }
}
