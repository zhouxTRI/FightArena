using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float safetyRadius = 1.2f;

        public float SafetyRadius => safetyRadius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                transform.position,
                safetyRadius);
        }
    }
}
