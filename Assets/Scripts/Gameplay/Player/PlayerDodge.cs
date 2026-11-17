using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerDodge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private LayerMask obstacleMask;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float cooldown = 1.2f;
        [SerializeField, Min(0.01f)] private float duration = 0.22f;
        [SerializeField, Min(0.01f)] private float recoveryDuration = 0.08f;
        [SerializeField, Min(0f)] private float invulnerableStart = 0.03f;
        [SerializeField, Min(0f)] private float invulnerableEnd = 0.20f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float distance = 3.2f;
        [SerializeField, Min(0f)] private float inputDeadzone = 0.15f;
        [SerializeField, Min(0f)] private float wallSkin = 0.02f;

        private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
        private ContactFilter2D contactFilter;
        private Vector2 dodgeDirection;
        private float elapsed;
        private float cooldownRemaining;
        private bool moving;
        private bool recovering;

        public bool IsActive => moving || recovering;
        public bool IsRecovering => recovering;
        public bool IsFinished => !moving && !recovering;
        public float CooldownRemaining => cooldownRemaining;

        public bool IsInvulnerable
        {
            get
            {
                return moving &&
                       elapsed >= invulnerableStart &&
                       elapsed <= invulnerableEnd;
            }
        }

        private void Awake()
        {
            if (motor == null || body == null || bodyCollider == null)
            {
                Debug.LogError(
                    "PlayerDodge requires PlayerMotor, Rigidbody2D " +
                    "and Collider2D references.",
                    this);
                enabled = false;
                return;
            }

            contactFilter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = obstacleMask
            };
        }

        public bool TryStart(Vector2 moveIntent, Vector2 aimDirection)
        {
            if (moving || recovering || cooldownRemaining > 0f)
            {
                return false;
            }

            dodgeDirection = ChooseDirection(moveIntent, aimDirection);

            if (dodgeDirection.sqrMagnitude <= 0.000001f)
            {
                dodgeDirection = Vector2.right;
            }

            dodgeDirection.Normalize();
            elapsed = 0f;
            cooldownRemaining = cooldown;
            moving = true;
            recovering = false;

            motor.BeginExternalVelocityControl();
            return true;
        }

        public void TickState(float deltaTime)
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(
                    0f,
                    cooldownRemaining - deltaTime);
            }

            if (!moving && !recovering)
            {
                return;
            }

            elapsed += deltaTime;

            if (moving && elapsed >= duration)
            {
                FinishMovement();
            }

            if (recovering && elapsed >= duration + recoveryDuration)
            {
                recovering = false;
            }
        }

        public void TickFixed(float deltaTime)
        {
            if (!moving || body == null || bodyCollider == null ||
                deltaTime <= 0f)
            {
                return;
            }

            float speed = distance / duration;
            float requestedStep = speed * deltaTime;
            float castDistance = requestedStep + wallSkin;

            int hitCount = bodyCollider.Cast(
                dodgeDirection,
                contactFilter,
                castHits,
                castDistance,
                true);

            float allowedDistance = requestedStep;

            if (hitCount > 0)
            {
                float nearestDistance = castDistance;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit2D hit = castHits[i];

                    if (hit.collider != null &&
                        hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                    }
                }

                allowedDistance = Mathf.Max(
                    0f,
                    nearestDistance - wallSkin);

                if (allowedDistance < 0.0001f)
                {
                    FinishMovement();
                    return;
                }
            }

            body.velocity = dodgeDirection * (allowedDistance / deltaTime);
        }

        public void Cancel()
        {
            if (moving || recovering)
            {
                FinishMovement();
                recovering = false;
                cooldownRemaining = Mathf.Max(
                    cooldownRemaining,
                    cooldown * 0.5f);
            }
        }

        private void FinishMovement()
        {
            if (!moving)
            {
                return;
            }

            moving = false;
            recovering = true;
            body.velocity = Vector2.zero;
            motor.EndExternalVelocityControl();
        }

        private Vector2 ChooseDirection(
            Vector2 moveIntent,
            Vector2 aimDirection)
        {
            if (moveIntent.sqrMagnitude >
                inputDeadzone * inputDeadzone)
            {
                return moveIntent.normalized;
            }

            if (aimDirection.sqrMagnitude > 0.000001f)
            {
                return aimDirection.normalized;
            }

            return Vector2.right;
        }

        private void OnDisable()
        {
            if (moving || recovering)
            {
                moving = false;
                recovering = false;
                body.velocity = Vector2.zero;
                motor.EndExternalVelocityControl();
            }
        }

        private void OnValidate()
        {
            cooldown = Mathf.Max(0f, cooldown);
            duration = Mathf.Max(0.01f, duration);
            recoveryDuration = Mathf.Max(0.01f, recoveryDuration);
            invulnerableStart = Mathf.Max(0f, invulnerableStart);
            invulnerableEnd = Mathf.Max(invulnerableStart, invulnerableEnd);
            distance = Mathf.Max(0f, distance);
            inputDeadzone = Mathf.Max(0f, inputDeadzone);
            wallSkin = Mathf.Max(0f, wallSkin);
        }
    }
}
