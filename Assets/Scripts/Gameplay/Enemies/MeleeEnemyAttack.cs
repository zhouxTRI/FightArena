using FightArena.Core.Combat;
using FightArena.Gameplay.Player;
using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class MeleeEnemyAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyHealth sourceHealth;
        [SerializeField] private PlayerHealth targetHealth;
        [SerializeField] private Transform target;
        [SerializeField] private LayerMask obstacleMask;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float attackRange = 0.9f;
        [SerializeField, Min(0f)] private float windup = 0.35f;
        [SerializeField, Min(0f)] private float attackDuration = 0.18f;
        [SerializeField, Min(0f)] private float recovery = 0.22f;
        [SerializeField, Min(0f)] private float cooldown = 1.25f;
        [SerializeField, Min(0)] private int damage = 12;

        private enum Phase
        {
            Idle,
            Windup,
            Attack,
            Recovery
        }

        private Phase phase;
        private float elapsed;
        private float cooldownRemaining;
        private bool damageApplied;

        public bool IsActive => phase != Phase.Idle;
        public bool IsInWindup => phase == Phase.Windup;
        public bool IsInAttack => phase == Phase.Attack;
        public bool IsInRecovery => phase == Phase.Recovery;
        public float AttackRange => attackRange;

        public void SetTarget(
            Transform targetTransform,
            PlayerHealth playerHealth)
        {
            target = targetTransform;
            targetHealth = playerHealth;
        }

        public bool CanStart(float distance)
        {
            return !IsActive &&
                   cooldownRemaining <= 0f &&
                   distance <= attackRange &&
                   IsTargetValid() &&
                   HasLineOfSight();
        }

        public void Begin()
        {
            if (!CanStart(GetTargetDistance()))
            {
                return;
            }

            phase = Phase.Windup;
            elapsed = 0f;
            damageApplied = false;
        }

        public void Tick(float deltaTime)
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(
                    0f,
                    cooldownRemaining - deltaTime);
            }

            if (phase == Phase.Idle)
            {
                return;
            }

            elapsed += deltaTime;

            if (phase == Phase.Windup &&
                elapsed >= windup)
            {
                ExecuteHit();
                phase = Phase.Attack;
            }

            if (phase == Phase.Attack &&
                elapsed >= windup + attackDuration)
            {
                phase = Phase.Recovery;
            }

            if (phase == Phase.Recovery &&
                elapsed >= windup + attackDuration + recovery)
            {
                phase = Phase.Idle;
                cooldownRemaining = cooldown;
            }
        }

        public void Cancel()
        {
            if (phase == Phase.Idle)
            {
                return;
            }

            phase = Phase.Idle;
            elapsed = 0f;
            cooldownRemaining = Mathf.Max(
                cooldownRemaining,
                0.2f);
        }

        private void ExecuteHit()
        {
            if (damageApplied || !IsTargetValid())
            {
                return;
            }

            damageApplied = true;

            if (GetTargetDistance() > attackRange + 0.15f ||
                !HasLineOfSight())
            {
                return;
            }

            Vector2 direction =
                ((Vector2)target.position -
                 (Vector2)transform.position).normalized;

            targetHealth.TryApplyDamage(new DamageInfo(
                sourceHealth.InstanceId,
                damage,
                target.position,
                direction));
        }

        private bool HasLineOfSight()
        {
            return Physics2D.Linecast(
                transform.position,
                target.position,
                obstacleMask).collider == null;
        }

        private float GetTargetDistance()
        {
            return target == null
                ? float.PositiveInfinity
                : Vector2.Distance(
                    transform.position,
                    target.position);
        }

        private bool IsTargetValid()
        {
            return sourceHealth != null &&
                   target != null &&
                   targetHealth != null &&
                   !targetHealth.IsDead &&
                   !sourceHealth.IsDead;
        }

        private void OnValidate()
        {
            attackRange = Mathf.Max(0f, attackRange);
            windup = Mathf.Max(0f, windup);
            attackDuration = Mathf.Max(0f, attackDuration);
            recovery = Mathf.Max(0f, recovery);
            cooldown = Mathf.Max(0f, cooldown);
            damage = Mathf.Max(0, damage);
        }
    }
}
