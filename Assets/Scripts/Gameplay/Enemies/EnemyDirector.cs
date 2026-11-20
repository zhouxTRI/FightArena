using System;
using System.Collections.Generic;
using FightArena.Core.Combat;
using FightArena.Gameplay.Combat;
using FightArena.Gameplay.Player;
using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemyDirector : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private EnemyAgent meleeEnemyPrefab;
        [SerializeField] private EnemyAgent rangedEnemyPrefab;

        [Header("Scene References")]
        [SerializeField] private Transform enemyParent;
        [SerializeField] private Transform target;
        [SerializeField] private PlayerHealth targetHealth;
        [SerializeField] private ProjectileService projectileService;
        [SerializeField] private EnemySpawnPoint[] spawnPoints;
        [SerializeField] private ArenaPathGrid pathGrid;
        [SerializeField] private FlowFieldPathfinder flowField;
        [SerializeField] private EnemySpatialHash spatialHash;

        [Header("Spawning")]
        [SerializeField] private bool spawnAutomatically = true;
        [SerializeField, Min(0)] private int maxActive = 20;
        [SerializeField, Min(0.05f)] private float spawnInterval = 1f;
        [SerializeField, Min(0f)] private float minPlayerDistance = 6f;
        [SerializeField, Min(0f)] private float spawnClearance = 1.2f;
        [SerializeField, Range(0f, 1f)] private float meleeChance = 0.65f;
        [SerializeField, Min(0f)] private float deathDespawnDelay = 2f;
        [SerializeField, Min(0.05f)] private float steeringInterval = 0.1f;
        [SerializeField, Min(0.1f)] private float separationRadius = 1.1f;

        [Header("Definition")]
        [SerializeField] private string meleeMonsterId = "melee_chushouguai";
        [SerializeField] private string rangedMonsterId = "ranged_outuzhe";
        [SerializeField, Min(1)] private int meleeHealth = 45;
        [SerializeField, Min(1)] private int rangedHealth = 32;

        private readonly List<EnemyAgent> activeEnemies =
            new List<EnemyAgent>();

        private readonly Dictionary<int, EnemyAgent> activeById =
            new Dictionary<int, EnemyAgent>();

        private readonly HashSet<int> processedDeathIds =
            new HashSet<int>();

        private int nextEnemyInstanceId;
        private float spawnTimer;
        private float steeringTimer;
        private bool initialized;

        public event Action<EnemyKilledInfo> EnemyKilled;

        private void Awake()
        {
            if (targetHealth == null && target != null)
            {
                targetHealth = target.GetComponentInChildren<PlayerHealth>();
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = FindObjectsOfType<EnemySpawnPoint>();
            }

            initialized =
                meleeEnemyPrefab != null &&
                rangedEnemyPrefab != null &&
                enemyParent != null &&
                target != null &&
                targetHealth != null &&
                projectileService != null &&
                pathGrid != null &&
                flowField != null &&
                spatialHash != null &&
                spawnPoints != null &&
                spawnPoints.Length > 0;

            if (!initialized)
            {
                Debug.LogError(
                    "EnemyDirector is missing required references.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!initialized || !spawnAutomatically)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f &&
                activeEnemies.Count < maxActive)
            {
                spawnTimer = spawnInterval;
                SpawnRandomEnemy();
            }
        }

        private void LateUpdate()
        {
            if (!initialized || activeEnemies.Count == 0)
            {
                return;
            }

            steeringTimer -= Time.deltaTime;

            if (steeringTimer > 0f)
            {
                return;
            }

            steeringTimer = steeringInterval;
            spatialHash.Rebuild(activeEnemies);

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyAgent enemy = activeEnemies[i];

                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                flowField.TryGetDirection(
                    enemy.transform.position,
                    out Vector2 flowDirection);

                Vector2 separation = spatialHash.CalculateSeparation(
                    enemy,
                    separationRadius);

                enemy.SetSteering(
                    flowDirection,
                    separation);
            }
        }

        public EnemyAgent SpawnMelee()
        {
            return Spawn(EnemyKind.Melee);
        }

        public EnemyAgent SpawnRanged()
        {
            return Spawn(EnemyKind.Ranged);
        }

        public EnemyAgent SpawnRandomEnemy()
        {
            return Spawn(
                UnityEngine.Random.value <= meleeChance
                    ? EnemyKind.Melee
                    : EnemyKind.Ranged);
        }

        public EnemyAgent Spawn(EnemyKind kind)
        {
            if (!initialized || activeEnemies.Count >= maxActive)
            {
                return null;
            }

            EnemySpawnPoint spawnPoint = FindSafeSpawnPoint();

            if (spawnPoint == null)
            {
                return null;
            }

            EnemyAgent prefab = kind == EnemyKind.Melee
                ? meleeEnemyPrefab
                : rangedEnemyPrefab;

            EnemyAgent enemy = Instantiate(
                prefab,
                spawnPoint.transform.position,
                Quaternion.identity,
                enemyParent);

            EnemyHealth health = enemy.Health;

            if (health == null)
            {
                Debug.LogError(
                    "Enemy prefab is missing EnemyHealth.",
                    enemy);
                Destroy(enemy.gameObject);
                return null;
            }

            nextEnemyInstanceId++;
            int instanceId = nextEnemyInstanceId;
            string monsterId = kind == EnemyKind.Melee
                ? meleeMonsterId
                : rangedMonsterId;
            int healthValue = kind == EnemyKind.Melee
                ? meleeHealth
                : rangedHealth;

            enemy.Configure(
                instanceId,
                monsterId,
                healthValue,
                target,
                targetHealth,
                projectileService);

            health.Died += OnEnemyDied;
            activeEnemies.Add(enemy);
            activeById[instanceId] = enemy;
            return enemy;
        }

        public void DespawnAll()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                EnemyAgent enemy = activeEnemies[i];

                if (enemy == null)
                {
                    continue;
                }

                if (enemy.Health != null)
                {
                    enemy.Health.Died -= OnEnemyDied;
                }

                Destroy(enemy.gameObject);
            }

            activeEnemies.Clear();
            activeById.Clear();
            processedDeathIds.Clear();
            spatialHash?.Clear();
        }

        private EnemySpawnPoint FindSafeSpawnPoint()
        {
            int startIndex = UnityEngine.Random.Range(
                0,
                spawnPoints.Length);

            int collisionMask = (1 << 8) | (1 << 11);

            for (int offset = 0;
                 offset < spawnPoints.Length;
                 offset++)
            {
                EnemySpawnPoint point =
                    spawnPoints[(startIndex + offset) %
                                spawnPoints.Length];

                if (point == null)
                {
                    continue;
                }

                Vector2 position = point.transform.position;

                if (Vector2.Distance(position, target.position) <
                    minPlayerDistance)
                {
                    continue;
                }

                if (!pathGrid.IsWalkable(position))
                {
                    continue;
                }

                float radius = Mathf.Max(
                    spawnClearance,
                    point.SafetyRadius);

                if (Physics2D.OverlapCircle(
                        position,
                        radius,
                        collisionMask) != null)
                {
                    continue;
                }

                return point;
            }

            return null;
        }

        private void OnEnemyDied(EnemyKilledInfo info)
        {
            if (!processedDeathIds.Add(info.EnemyInstanceId))
            {
                return;
            }

            if (!activeById.TryGetValue(
                    info.EnemyInstanceId,
                    out EnemyAgent enemy))
            {
                return;
            }

            activeById.Remove(info.EnemyInstanceId);
            activeEnemies.Remove(enemy);

            if (enemy != null && enemy.Health != null)
            {
                enemy.Health.Died -= OnEnemyDied;
            }

            EnemyKilled?.Invoke(info);

            if (enemy != null)
            {
                Destroy(
                    enemy.gameObject,
                    deathDespawnDelay);
            }
        }

        private void OnDestroy()
        {
            EnemyKilled = null;
        }
    }
}
