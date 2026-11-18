using System;
using System.Collections.Generic;
using FightArena.Core.Combat;
using UnityEngine;

namespace FightArena.Gameplay.Combat
{
    public sealed class ProjectileService : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private ProjectileRuntime playerProjectilePrefab;
        [SerializeField] private ProjectileRuntime enemyProjectilePrefab;

        [Header("Containers")]
        [SerializeField] private Transform playerProjectileParent;
        [SerializeField] private Transform enemyProjectileParent;

        [Header("Pool")]
        [SerializeField, Min(0)] private int prewarmPerTeam = 24;

        private sealed class PoolState
        {
            public readonly Queue<ProjectileRuntime> Available =
                new Queue<ProjectileRuntime>();
        }

        private readonly Dictionary<ProjectileTeam, PoolState> pools =
            new Dictionary<ProjectileTeam, PoolState>();

        private readonly Dictionary<ProjectileRuntime, ProjectileTeam>
            projectileTeams =
                new Dictionary<ProjectileRuntime, ProjectileTeam>();

        private readonly List<ProjectileRuntime> activeProjectiles =
            new List<ProjectileRuntime>();

        private int nextProjectileId;
        private bool initialized;

        public event Action<ProjectileHitResult> ProjectileHit;

        private void Awake()
        {
            initialized = ValidateReferences();

            if (!initialized)
            {
                enabled = false;
                return;
            }

            pools[ProjectileTeam.Player] = new PoolState();
            pools[ProjectileTeam.Enemy] = new PoolState();

            Prewarm(
                ProjectileTeam.Player,
                playerProjectilePrefab,
                playerProjectileParent);

            Prewarm(
                ProjectileTeam.Enemy,
                enemyProjectilePrefab,
                enemyProjectileParent);
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                ProjectileRuntime projectile = activeProjectiles[i];

                if (projectile == null)
                {
                    activeProjectiles.RemoveAt(i);
                    continue;
                }

                projectile.TickFixed(deltaTime);
            }
        }

        public ProjectileRuntime SpawnPlayerProjectile(
            in ProjectileLaunchData data)
        {
            return Spawn(ProjectileTeam.Player, data);
        }

        public ProjectileRuntime SpawnEnemyProjectile(
            in ProjectileLaunchData data)
        {
            return Spawn(ProjectileTeam.Enemy, data);
        }

        public void DespawnAll()
        {
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                Release(activeProjectiles[i]);
            }
        }

        internal void Release(ProjectileRuntime projectile)
        {
            if (projectile == null)
            {
                return;
            }

            activeProjectiles.Remove(projectile);

            if (!projectileTeams.TryGetValue(
                    projectile,
                    out ProjectileTeam team))
            {
                Destroy(projectile.gameObject);
                return;
            }

            projectileTeams.Remove(projectile);
            projectile.Deactivate();

            Transform parent = GetParent(team);
            projectile.transform.SetParent(parent, false);
            pools[team].Available.Enqueue(projectile);
        }

        internal void ReportHit(
            int projectileId,
            ProjectileTeam team,
            Vector2 point,
            Collider2D collider,
            bool hitWall,
            bool damageApplied)
        {
            ProjectileHit?.Invoke(new ProjectileHitResult(
                projectileId,
                team,
                point,
                collider,
                hitWall,
                damageApplied));
        }

        private ProjectileRuntime Spawn(
            ProjectileTeam team,
            in ProjectileLaunchData data)
        {
            if (!initialized)
            {
                return null;
            }

            PoolState pool = pools[team];
            ProjectileRuntime prefab = GetPrefab(team);
            Transform parent = GetParent(team);

            ProjectileRuntime projectile = pool.Available.Count > 0
                ? pool.Available.Dequeue()
                : Instantiate(prefab, parent);

            projectile.transform.SetParent(parent, false);
            projectile.gameObject.SetActive(true);

            nextProjectileId++;
            activeProjectiles.Add(projectile);
            projectileTeams[projectile] = team;

            projectile.Launch(
                data,
                this,
                team,
                nextProjectileId);

            return projectile;
        }

        private void Prewarm(
            ProjectileTeam team,
            ProjectileRuntime prefab,
            Transform parent)
        {
            PoolState pool = pools[team];

            for (int i = 0; i < prewarmPerTeam; i++)
            {
                ProjectileRuntime projectile = Instantiate(prefab, parent);
                projectile.gameObject.SetActive(false);
                pool.Available.Enqueue(projectile);
            }
        }

        private ProjectileRuntime GetPrefab(ProjectileTeam team)
        {
            return team == ProjectileTeam.Player
                ? playerProjectilePrefab
                : enemyProjectilePrefab;
        }

        private Transform GetParent(ProjectileTeam team)
        {
            return team == ProjectileTeam.Player
                ? playerProjectileParent
                : enemyProjectileParent;
        }

        private bool ValidateReferences()
        {
            if (playerProjectilePrefab != null &&
                enemyProjectilePrefab != null &&
                playerProjectileParent != null &&
                enemyProjectileParent != null)
            {
                return true;
            }

            Debug.LogError(
                "ProjectileService is missing prefab or container references.",
                this);

            return false;
        }
    }
}
