using FightArena.Core.Combat;
using FightArena.Gameplay.Combat;
using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerWeapon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerAim aim;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ProjectileService projectileService;
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Weapon")]
        [SerializeField, Min(0f)] private int damage = 18;
        [SerializeField, Min(0.01f)] private float fireRate = 6f;
        [SerializeField, Min(0f)] private float projectileSpeed = 28f;
        [SerializeField, Min(0f)] private float maxDistance = 16f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 2f;

        private float cooldownRemaining;

        public bool CanFire => cooldownRemaining <= 0f;

        private void Awake()
        {
            if (aim == null || muzzle == null ||
                projectileService == null || playerHealth == null)
            {
                Debug.LogError(
                    "PlayerWeapon is missing required references.",
                    this);
                enabled = false;
            }
        }

        public void TickWeapon(bool fireHeld, bool canFire)
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(
                    0f,
                    cooldownRemaining - Time.deltaTime);
            }

            if (!fireHeld || !canFire || cooldownRemaining > 0f)
            {
                return;
            }

            Fire();
            cooldownRemaining = 1f / fireRate;
        }

        private void Fire()
        {
            Vector2 direction = aim.AimDirection;

            if (direction.sqrMagnitude <= 0.000001f)
            {
                direction = Vector2.right;
            }

            ProjectileLaunchData data = new ProjectileLaunchData(
                ProjectileTeam.Player,
                playerHealth.InstanceId,
                muzzle.position,
                direction,
                damage,
                projectileSpeed,
                maxDistance,
                projectileLifetime);

            projectileService.SpawnPlayerProjectile(data);
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0, damage);
            fireRate = Mathf.Max(0.01f, fireRate);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            maxDistance = Mathf.Max(0f, maxDistance);
            projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
        }
    }
}
