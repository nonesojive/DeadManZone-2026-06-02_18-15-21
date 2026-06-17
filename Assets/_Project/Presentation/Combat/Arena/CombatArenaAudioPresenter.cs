using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>One-shot combat SFX driven from replay events (rifle, cannon, impact, death).</summary>
    public sealed class CombatArenaAudioPresenter : MonoBehaviour
    {
        [SerializeField] private CombatArenaAudioSetSO audioSet;
        [SerializeField] private float masterVolume = 0.55f;

        private void Awake()
        {
            if (audioSet == null)
                audioSet = Resources.Load<CombatArenaAudioSetSO>("DeadManZone/CombatArenaAudioSet");
        }

        public void Configure(CombatArenaAudioSetSO set)
        {
            if (set != null)
                audioSet = set;
        }

        public void PlayRifleShot(Vector3 worldPosition) =>
            PlayAt(audioSet?.rifleShot, worldPosition, pitchVariance: 0.08f);

        public void PlayCannonShot(Vector3 worldPosition) =>
            PlayAt(audioSet?.cannonShot, worldPosition, pitchVariance: 0.04f);

        public void PlayImpact(Vector3 worldPosition) =>
            PlayAt(audioSet?.bulletImpact, worldPosition, pitchVariance: 0.12f);

        public void PlayExplosion(Vector3 worldPosition) =>
            PlayAt(audioSet?.explosion, worldPosition, pitchVariance: 0.06f);

        public void PlayDeath(Vector3 worldPosition) =>
            PlayAt(audioSet?.unitDeath, worldPosition, pitchVariance: 0.05f);

        private void PlayAt(AudioClip clip, Vector3 worldPosition, float pitchVariance)
        {
            if (clip == null)
                return;

            // Never move this component's transform — it lives on the combat UI panel.
            AudioSource.PlayClipAtPoint(clip, worldPosition, masterVolume);
        }
    }
}
