using DeadManZone.Presentation.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>DEV-ONLY: opens the previous battle log review overlay. Remove before public release.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class LastBattleLogReviewButton : MonoBehaviour
    {
        [SerializeField] private LastBattleLogReviewPresenter reviewPresenter;

        private void Awake()
        {
            if (reviewPresenter == null)
                reviewPresenter = FindAnyObjectByType<LastBattleLogReviewPresenter>();

            GetComponent<Button>().onClick.AddListener(OnClicked);
        }

        private void OnClicked() => reviewPresenter?.Open();
    }
}
