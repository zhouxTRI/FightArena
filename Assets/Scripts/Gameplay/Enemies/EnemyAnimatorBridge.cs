using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemyAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedHash =
            Animator.StringToHash("Speed");
        private static readonly int AttackHash =
            Animator.StringToHash("Attack");
        private static readonly int IsDeadHash =
            Animator.StringToHash("IsDead");

        [SerializeField] private Animator animator;

        private EnemyState previousState = EnemyState.Spawn;
        private bool initialized;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            initialized = animator != null;

            if (!initialized)
            {
                Debug.LogError(
                    "EnemyAnimatorBridge requires an Animator.",
                    this);
                enabled = false;
            }
        }

        public void Render(EnemyState state, float speed01)
        {
            if (!initialized)
            {
                return;
            }

            float effectiveSpeed =
                state == EnemyState.Chase ||
                state == EnemyState.Recover
                    ? Mathf.Clamp01(speed01)
                    : 0f;

            animator.SetFloat(
                SpeedHash,
                effectiveSpeed,
                0.05f,
                Time.deltaTime);

            animator.SetBool(
                IsDeadHash,
                state == EnemyState.Dead);

            if (state == EnemyState.AttackWindup &&
                previousState != EnemyState.AttackWindup)
            {
                animator.ResetTrigger(AttackHash);
                animator.SetTrigger(AttackHash);
            }

            previousState = state;
        }

        public void ResetForSpawn()
        {
            if (!initialized)
            {
                return;
            }

            animator.ResetTrigger(AttackHash);
            animator.SetBool(IsDeadHash, false);
            animator.SetFloat(SpeedHash, 0f);
            animator.Play("Idle", 0, 0f);
            previousState = EnemyState.Spawn;
        }
    }
}
