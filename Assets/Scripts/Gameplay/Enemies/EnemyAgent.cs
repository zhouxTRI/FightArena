using FightArena.Core.Combat;
using FightArena.Gameplay.Combat;
using FightArena.Gameplay.Player;
using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemyAgent : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private EnemyKind kind = EnemyKind.Melee;
        [SerializeField, Min(0f)] private float aggroRange = 14f;
        [SerializeField, Min(0f)] private float decisionInterval = 0.1f;

        [Header("References")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyMotor motor;
        [SerializeField] private EnemyAnimatorBridge animatorBridge;
        [SerializeField] private MeleeEnemyAttack meleeAttack;
        [SerializeField] private RangedEnemyAttack rangedAttack;
        [SerializeField] private Transform target;
        [SerializeField] private PlayerHealth targetHealth;

        private EnemyState state = EnemyState.Spawn;
        private float decisionTimer;
        private float targetDistance = float.PositiveInfinity;
        private Vector2 flowDirection;
        private Vector2 separationDirection;
        private bool retreating;
        private bool initialized;

        public EnemyState State => state;
        public EnemyKind Kind => kind;
        public EnemyHealth Health => health;
        public float AggroRange => aggroRange;
        public bool IsDead => health != null && health.IsDead;

        private void Awake()
        {
            initialized =
                health != null &&
                motor != null &&
                animatorBridge != null;

            if (!initialized)
            {
                Debug.LogError(
                    "EnemyAgent is missing required references.",
                    this);
                enabled = false;
                return;
            }

            health.Died += OnEnemyDied;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= OnEnemyDied;
            }
        }

        public void Configure(
            int instanceId,
            string monsterId,
            int maxHealth,
            Transform targetTransform,
            PlayerHealth playerHealth,
            ProjectileService projectileService)
        {
            target = targetTransform;
            targetHealth = playerHealth;

            health.ResetForSpawn(
                instanceId,
                monsterId,
                kind,
                maxHealth);

            if (meleeAttack != null)
            {
                meleeAttack.SetTarget(
                    targetTransform,
                    playerHealth);
                meleeAttack.Cancel();
            }

            if (rangedAttack != null)
            {
                rangedAttack.SetTarget(
                    targetTransform,
                    playerHealth);
                rangedAttack.SetProjectileService(
                    projectileService);
                rangedAttack.Cancel();
            }

            state = EnemyState.Spawn;
            decisionTimer = 0f;
            flowDirection = Vector2.zero;
            separationDirection = Vector2.zero;
            retreating = false;

            animatorBridge.ResetForSpawn();
            initialized = true;
            enabled = true;
        }

        public void SetSteering(
            Vector2 desiredFlowDirection,
            Vector2 desiredSeparation)
        {
            flowDirection = desiredFlowDirection;
            separationDirection = desiredSeparation;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (health.IsDead)
            {
                EnterDeadState();
                return;
            }

            if (target == null || targetHealth == null)
            {
                state = EnemyState.Chase;
                animatorBridge.Render(state, motor.Speed01);
                return;
            }

            targetDistance = Vector2.Distance(
                transform.position,
                target.position);

            TickActiveAttack(Time.deltaTime);

            if (IsAnyAttackActive())
            {
                UpdateAttackState();
                animatorBridge.Render(state, 0f);
                return;
            }

            decisionTimer -= Time.deltaTime;

            if (decisionTimer <= 0f)
            {
                decisionTimer = decisionInterval;
                MakeDecision();
            }

            animatorBridge.Render(state, motor.Speed01);
        }

        private void FixedUpdate()
        {
            if (!initialized || health.IsDead)
            {
                return;
            }

            Vector2 movementDirection = retreating &&
                                        target != null
                ? ((Vector2)transform.position -
                   (Vector2)target.position).normalized
                : flowDirection;
            Vector2 movementSeparation = separationDirection;

            if (IsAnyAttackActive() ||
                state == EnemyState.AttackWindup ||
                state == EnemyState.Attack ||
                state == EnemyState.Recover)
            {
                movementDirection = Vector2.zero;
                movementSeparation = Vector2.zero;
            }

            motor.TickFixed(
                movementDirection,
                movementSeparation,
                Time.fixedDeltaTime);
        }

        private void MakeDecision()
        {
            retreating = false;

            if (targetDistance > aggroRange)
            {
                state = EnemyState.Chase;
                return;
            }

            if (kind == EnemyKind.Melee &&
                meleeAttack != null &&
                meleeAttack.CanStart(targetDistance))
            {
                meleeAttack.Begin();
                state = EnemyState.AttackWindup;
                return;
            }

            if (kind == EnemyKind.Ranged &&
                rangedAttack != null)
            {
                if (targetDistance < rangedAttack.MinRange)
                {
                    retreating = true;
                    state = EnemyState.Chase;
                    return;
                }

                if (rangedAttack.CanStart(targetDistance))
                {
                    rangedAttack.Begin();
                    state = EnemyState.AttackWindup;
                    return;
                }
            }

            state = EnemyState.Chase;
        }

        private void TickActiveAttack(float deltaTime)
        {
            if (meleeAttack != null)
            {
                meleeAttack.Tick(deltaTime);
            }

            if (rangedAttack != null)
            {
                rangedAttack.Tick(deltaTime);
            }
        }

        private void UpdateAttackState()
        {
            if (kind == EnemyKind.Melee && meleeAttack != null)
            {
                state = meleeAttack.IsInWindup
                    ? EnemyState.AttackWindup
                    : meleeAttack.IsInAttack
                        ? EnemyState.Attack
                        : EnemyState.Recover;
                return;
            }

            if (kind == EnemyKind.Ranged && rangedAttack != null)
            {
                state = rangedAttack.IsInWindup
                    ? EnemyState.AttackWindup
                    : EnemyState.Recover;
            }
        }

        private bool IsAnyAttackActive()
        {
            return (meleeAttack != null && meleeAttack.IsActive) ||
                   (rangedAttack != null && rangedAttack.IsActive);
        }

        private void OnEnemyDied(EnemyKilledInfo info)
        {
            EnterDeadState();
        }

        private void EnterDeadState()
        {
            if (state == EnemyState.Dead)
            {
                return;
            }

            state = EnemyState.Dead;
            retreating = false;
            motor.StopImmediately();

            if (meleeAttack != null)
            {
                meleeAttack.Cancel();
            }

            if (rangedAttack != null)
            {
                rangedAttack.Cancel();
            }

            animatorBridge.Render(EnemyState.Dead, 0f);
        }

        private void OnValidate()
        {
            aggroRange = Mathf.Max(0f, aggroRange);
            decisionInterval = Mathf.Max(0.01f, decisionInterval);
        }
    }
}
