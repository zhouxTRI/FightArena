using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float maxSpeed = 6f;
        [SerializeField, Min(0f)] private float acceleration = 55f;
        [SerializeField, Min(0f)] private float deceleration = 70f;                     //当玩家没有输入时的减速度
        [SerializeField, Min(0f)] private float reverseAcceleration = 90f;              //当玩家输入与当前速度方向相反时的加速度
        [SerializeField, Range(0f, 1f)] private float inputDeadzone = 0.15f;            //输入死区，低于此值的输入将被忽略

        private Vector2 moveIntent;                                                     //玩家的移动意图，表示玩家希望移动的方向和强度
        private bool movementEnabled = true;
        private bool externalVelocityControl;                                           //当外部系统影响玩家的速度时，PlayerMotor将不会修改Rigidbody2D的速度

        public Vector2 MoveIntent => moveIntent;

        public float MoveSpeed01                                                        //玩家当前速度与最大速度的比值，范围在0到1之间；用于动画系统或其他需要知道玩家速度的地方
        {
            get
            {
                if (body == null || maxSpeed <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(body.velocity.magnitude / maxSpeed);
            }
        }

        private void Awake()
        {
            if (body == null)
            {
                Debug.LogError("PlayerMotor requires a Rigidbody2D.", this);
                enabled = false;
            }
        }

        public void SetMoveIntent(Vector2 input)
        {
            moveIntent = Vector2.ClampMagnitude(input, 1f);                 
        }

        public void SetMovementEnabled(bool enabledValue)                   
        {
            movementEnabled = enabledValue;

            if (!movementEnabled)
            {
                moveIntent = Vector2.zero;
            }
        }

        //当外部系统需要完全控制玩家的速度时，调用BeginExternalVelocityControl()，PlayerMotor将不会修改Rigidbody2D的速度；当外部系统不再需要控制玩家的速度时，调用EndExternalVelocityControl()，PlayerMotor将恢复对Rigidbody2D的速度控制
        public void BeginExternalVelocityControl()
        {
            externalVelocityControl = true;
        }

        public void EndExternalVelocityControl()
        {
            externalVelocityControl = false;
        }

        //在FixedUpdate()中调用此方法，处理玩家的移动逻辑，包括根据玩家的输入计算目标速度，并使用加速度和减速度平滑地调整Rigidbody2D的速度
        public void TickFixed(float deltaTime)
        {
            if (body == null || externalVelocityControl)
            {
                return;
            }

            Vector2 input = movementEnabled ? moveIntent : Vector2.zero;

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            if (input.sqrMagnitude <= inputDeadzone * inputDeadzone)
            {
                input = Vector2.zero;
            }

            Vector2 targetVelocity = input * maxSpeed;
            Vector2 currentVelocity = body.velocity;
            float rate;                                                                     //根据当前速度和目标速度的关系，选择使用加速度、减速度或反向加速度来调整速度

            if (targetVelocity.sqrMagnitude <= 0.0001f)                                     //如果目标速度接近于零，则使用减速度来减速
            {
                rate = deceleration;
            }
            else if (Vector2.Dot(currentVelocity, targetVelocity) < 0f)                     //用Dot判断当前速度和目标速度是否相反，如果相反，则使用反向加速度
            {
                rate = reverseAcceleration;
            }
            else
            {
                rate = acceleration;
            }

            body.velocity = Vector2.MoveTowards(                                            //使用MoveTowards平滑地调整当前速度到目标速度，速度变化率为rate * deltaTime
                currentVelocity,
                targetVelocity,
                rate * deltaTime);
        }

        private void OnDisable()
        {
            externalVelocityControl = false;
            moveIntent = Vector2.zero;

            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                body.velocity = Vector2.zero;
            }
        }

        private void OnValidate()
        {
            maxSpeed = Mathf.Max(0f, maxSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            reverseAcceleration = Mathf.Max(0f, reverseAcceleration);
            inputDeadzone = Mathf.Clamp01(inputDeadzone);
        }
    }
}
