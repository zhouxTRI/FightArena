using FightArena.Core.Combat;
using FightArena.Gameplay.Combat;
using FightArena.Gameplay.Player;
using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class RangedEnemyAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyHealth sourceHealth;
        [SerializeField] private PlayerHealth targetHealth;
        [SerializeField] private Transform target;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ProjectileService projectileService;
        [SerializeField] private LayerMask obstacleMask;

        [Header("Range")]
        [SerializeField, Min(0f)] private float minRange = 4.5f;
        [SerializeField, Min(0f)] private float maxRange = 8.5f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float windup = 0.55f;
        [SerializeField, Min(0f)] private float recovery = 0.3f;
        [SerializeField, Min(0f)] private float cooldown = 1.8f;
        [SerializeField, Min(0)] private int damage = 8;
        [SerializeField, Min(0f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0f)] private float projectileMaxDistance = 12f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 2f;

        private enum Phase
        {
            Idle,
            Windup,
            Recovery
        }

        private Phase phase;
        private float elapsed;
        private float cooldownRemaining;
        private bool shotFired;

        public bool IsActive => phase != Phase.Idle;
        public bool IsInWindup => phase == Phase.Windup;
        public bool IsInAttack => false;
        public bool IsInRecovery => phase == Phase.Recovery;
        public float MinRange => minRange;
        public float MaxRange => maxRange;

        public void SetTarget(
            Transform targetTransform,
            PlayerHealth playerHealth)
        {
            target = targetTransform;
            targetHealth = playerHealth;
        }

        public void SetProjectileService(
            ProjectileService service)
        {
            projectileService = service;
        }

        public bool CanStart(float distance)
        {
            return !IsActive &&
                   cooldownRemaining <= 0f &&
                   distance >= minRange &&
                   distance <= maxRange &&
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
            shotFired = false;
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
                FireIfValid();
                phase = Phase.Recovery;
            }

            if (phase == Phase.Recovery &&
                elapsed >= windup + recovery)
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
                0.25f);
        }

        private void FireIfValid()
        {
            if (shotFired ||
                !IsTargetValid() ||
                projectileService == null)
            {
                return;
            }

            shotFired = true;
            float distance = GetTargetDistance();

            if (distance < minRange ||
                distance > maxRange ||
                !HasLineOfSight())
            {
                return;
            }

            Vector2 direction =
                ((Vector2)target.position -
                 (Vector2)muzzle.position).normalized;

            projectileService.SpawnEnemyProjectile(
                new ProjectileLaunchData(
                    ProjectileTeam.Enemy,
                    sourceHealth.InstanceId,
                    muzzle.position,
                    direction,
                    damage,
                    projectileSpeed,
                    projectileMaxDistance,
                    projectileLifetime));
        }

        private bool HasLineOfSight()
        {
            if (target == null || muzzle == null)
            {
                return false;
            }

            return Physics2D.Linecast(
                muzzle.position,
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
                   muzzle != null &&
                   projectileService != null &&
                   !targetHealth.IsDead &&
                   !sourceHealth.IsDead;
        }

        private void OnValidate()
        {
            minRange = Mathf.Max(0f, minRange);
            maxRange = Mathf.Max(minRange, maxRange);
            windup = Mathf.Max(0f, windup);
            recovery = Mathf.Max(0f, recovery);
            cooldown = Mathf.Max(0f, cooldown);
            damage = Mathf.Max(0, damage);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileMaxDistance = Mathf.Max(0f, projectileMaxDistance);
            projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
        }
    }
}
