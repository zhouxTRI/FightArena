using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemyMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private LayerMask obstacleMask;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float maxSpeed = 2.2f;
        [SerializeField, Min(0f)] private float acceleration = 12f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField, Min(0f)] private float separationWeight = 0.55f;
        [SerializeField, Min(0f)] private float obstacleSkin = 0.03f;

        private readonly RaycastHit2D[] castHits =
            new RaycastHit2D[8];

        private ContactFilter2D contactFilter;
        private Vector2 currentDirection = Vector2.right;
        private float currentSpeed;
        private bool initialized;

        public Vector2 CurrentDirection => currentDirection;

        public float Speed01
        {
            get
            {
                return maxSpeed <= 0f
                    ? 0f
                    : Mathf.Clamp01(currentSpeed / maxSpeed);
            }
        }

        private void Awake()
        {
            initialized =
                body != null &&
                bodyCollider != null &&
                visualRoot != null;

            if (!initialized)
            {
                Debug.LogError(
                    "EnemyMotor requires Rigidbody2D, Collider2D " +
                    "and VisualRoot references.",
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

        public void TickFixed(
            Vector2 flowDirection,
            Vector2 separation,
            float deltaTime)
        {
            if (!initialized || deltaTime <= 0f)
            {
                return;
            }

            Vector2 desiredDirection = flowDirection.normalized +
                separation * separationWeight;

            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    0f,
                    acceleration * deltaTime);
                body.velocity = currentDirection * currentSpeed;
                return;
            }

            desiredDirection.Normalize();

            float maxTurn = turnSpeed * deltaTime;
            currentDirection = Vector3.RotateTowards(
                currentDirection,
                desiredDirection,
                maxTurn * Mathf.Deg2Rad,
                0f);

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                maxSpeed,
                acceleration * deltaTime);

            float requestedStep = currentSpeed * deltaTime;
            Vector2 finalDirection = FindClearDirection(
                currentDirection,
                requestedStep);

            float speedScale = 1f;

            if (finalDirection.sqrMagnitude <= 0.0001f)
            {
                currentSpeed = 0f;
                body.velocity = Vector2.zero;
                return;
            }

            float castDistance = requestedStep + obstacleSkin;
            int hitCount = bodyCollider.Cast(
                finalDirection,
                contactFilter,
                castHits,
                castDistance,
                true);

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

                if (requestedStep > 0f)
                {
                    speedScale = Mathf.Clamp01(
                        Mathf.Max(
                            0f,
                            nearestDistance - obstacleSkin) /
                        requestedStep);
                }
            }

            body.velocity = finalDirection *
                            currentSpeed *
                            speedScale;

            if (Mathf.Abs(finalDirection.x) > 0.05f)
            {
                Vector3 scale = visualRoot.localScale;
                scale.x = Mathf.Abs(scale.x) *
                          (finalDirection.x >= 0f ? 1f : -1f);
                visualRoot.localScale = scale;
            }
        }

        public void StopImmediately()
        {
            currentSpeed = 0f;

            if (body != null)
            {
                body.velocity = Vector2.zero;
            }
        }

        private Vector2 FindClearDirection(
            Vector2 desiredDirection,
            float distance)
        {
            float castDistance = distance + obstacleSkin;

            if (!WouldHitObstacle(
                    desiredDirection,
                    castDistance))
            {
                return desiredDirection;
            }

            float[] angles = { 45f, -45f, 90f, -90f };

            for (int i = 0; i < angles.Length; i++)
            {
                Vector2 candidate = Quaternion.Euler(
                    0f,
                    0f,
                    angles[i]) * desiredDirection;

                if (!WouldHitObstacle(candidate, castDistance))
                {
                    return candidate;
                }
            }

            return Vector2.zero;
        }

        private bool WouldHitObstacle(
            Vector2 direction,
            float distance)
        {
            int hitCount = bodyCollider.Cast(
                direction,
                contactFilter,
                castHits,
                distance,
                true);

            for (int i = 0; i < hitCount; i++)
            {
                if (castHits[i].collider != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDisable()
        {
            StopImmediately();
        }

        private void OnValidate()
        {
            maxSpeed = Mathf.Max(0f, maxSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            separationWeight = Mathf.Max(0f, separationWeight);
            obstacleSkin = Mathf.Max(0f, obstacleSkin);
        }
    }
}
