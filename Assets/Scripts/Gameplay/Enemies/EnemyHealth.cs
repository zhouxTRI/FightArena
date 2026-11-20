using System;
using FightArena.Core.Combat;
using FightArena.Gameplay.Combat;
using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemyHealth :
        MonoBehaviour,
        IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 45;
        [SerializeField] private HitFlash2D hitFlash;

        private int currentHealth;
        private bool initialized;
        private bool isDead;

        public int InstanceId { get; private set; }
        public string MonsterId { get; private set; }
        public EnemyKind Kind { get; private set; }
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;

        public event Action<DamageInfo> Damaged;
        public event Action<EnemyKilledInfo> Died;

        public void Initialize(
            int instanceId,
            string monsterId,
            EnemyKind kind,
            int health)
        {
            InstanceId = instanceId;
            MonsterId = monsterId;
            Kind = kind;
            maxHealth = Mathf.Max(1, health);
            currentHealth = maxHealth;
            isDead = false;
            initialized = true;
        }

        public void ResetForSpawn(
            int instanceId,
            string monsterId,
            EnemyKind kind,
            int health)
        {
            Initialize(instanceId, monsterId, kind, health);
        }

        public bool TryApplyDamage(in DamageInfo damage)
        {
            if (!initialized || isDead || damage.Amount <= 0)
            {
                return false;
            }

            currentHealth = Mathf.Max(
                0,
                currentHealth - damage.Amount);

            if (hitFlash != null)
            {
                hitFlash.Flash();
            }

            Damaged?.Invoke(damage);

            if (currentHealth == 0)
            {
                Die(damage.SourceInstanceId);
            }

            return true;
        }

        private void Die(int killerInstanceId)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Died?.Invoke(new EnemyKilledInfo(
                InstanceId,
                MonsterId,
                (int)Kind,
                transform.position,
                killerInstanceId));
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
        }
    }
}
