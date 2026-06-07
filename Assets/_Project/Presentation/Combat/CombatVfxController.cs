using DeadManZone.Core.Run;
using UnityEngine;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>Spawns lightweight combat feedback effects from combat event log.</summary>
    public sealed class CombatVfxController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem impactBurstPrefab;
        [SerializeField] private ParticleSystem deathBurstPrefab;

        public void PlayImpact(Vector3 worldPosition)
        {
            if (impactBurstPrefab == null)
                return;

            var burst = Instantiate(impactBurstPrefab, worldPosition, Quaternion.identity, transform);
            burst.Play();
            Destroy(burst.gameObject, 2f);
        }

        public void PlayDeath(Vector3 worldPosition)
        {
            if (deathBurstPrefab == null)
                return;

            var burst = Instantiate(deathBurstPrefab, worldPosition, Quaternion.identity, transform);
            burst.Play();
            Destroy(burst.gameObject, 2.5f);
        }

        public void HandleCombatEvent(CombatEventRecord record, Vector3 worldPosition)
        {
            if (record == null)
                return;

            switch (record.ActionType)
            {
                case "damage":
                    PlayImpact(worldPosition);
                    break;
                case "destroyed":
                    PlayDeath(worldPosition);
                    break;
            }
        }
    }
}
