using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class TopTroopsBattlefieldView : MonoBehaviour
    {
        [SerializeField] private Transform gridRoot;

        public int CellCount => gridRoot != null ? gridRoot.childCount : 0;

        public void SetGridRoot(Transform root) => gridRoot = root;

        public Transform GetCell(int x, int z)
        {
            if (gridRoot == null)
                return null;

            return gridRoot.Find($"Cell_{x}_{z}");
        }
    }
}
