using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.DragDrop;
using UnityEngine;

namespace DeadManZone.Presentation.Bench
{
    public sealed class BenchView : MonoBehaviour
    {
        [SerializeField] private BenchSlotView[] slots;

        private ContentDatabase _database;

        private void Awake() => _database = ContentDatabase.Load();

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
        }

        private void OnRunStateChanged(Core.Run.RunState _) => Refresh();

        public void Refresh()
        {
            if (slots == null || RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            var registry = _database != null ? _database.BuildRegistry() : ContentDatabase.Load()?.BuildRegistry();
            if (registry == null)
                return;

            var bench = RunManager.Instance.State.BenchPieceIds;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                if (i < bench.Count)
                {
                    var pieceId = bench[i];
                    slots[i].Bind(i, pieceId, registry.GetById(pieceId));
                }
                else
                {
                    slots[i].Bind(i, null, null);
                }
            }
        }
    }
}
