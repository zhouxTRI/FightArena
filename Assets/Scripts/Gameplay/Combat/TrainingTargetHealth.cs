using System.Collections;
using FightArena.Core.Combat;
using UnityEngine;

namespace FightArena.Gameplay.Combat
{
    public sealed class TrainingTargetHealth :
        MonoBehaviour,
        IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(0f)] private float resetDelay = 0.75f;
        [SerializeField] private HitFlash2D hitFlash;

        private int currentHealth;
        private bool isResetting;
        private Coroutine resetRoutine;

        public int InstanceId => gameObject.GetInstanceID();
        public int CurrentHealth => currentHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public bool TryApplyDamage(in DamageInfo damage)
        {
            if (isResetting || damage.Amount <= 0)
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - damage.Amount);

            if (hitFlash != null)
            {
                hitFlash.Flash();
            }

            if (currentHealth == 0)
            {
                isResetting = true;
                resetRoutine = StartCoroutine(ResetAfterDelay());
            }

            return true;
        }

        private IEnumerator ResetAfterDelay()
        {
            yield return new WaitForSeconds(resetDelay);
            currentHealth = maxHealth;
            isResetting = false;
            resetRoutine = null;
        }

        private void OnDisable()
        {
            if (resetRoutine != null)
            {
                StopCoroutine(resetRoutine);
                resetRoutine = null;
            }

            isResetting = false;
            currentHealth = maxHealth;
        }
    }
}
