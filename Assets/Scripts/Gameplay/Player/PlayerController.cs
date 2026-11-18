using FightArena.Core.Input;
using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Player Components")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private PlayerWeapon playerWeapon;
        [SerializeField] private PlayerAim playerAim;
        [SerializeField] private PlayerDodge playerDodge;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerAnimatorBridge animatorBridge;

        [Header("State")]
        [SerializeField, Min(0f)] private float hurtStateDuration = 0.15f;

        private PlayerState state = PlayerState.Normal;
        private float hurtRemaining;
        private bool initialized;

        public PlayerState State => state;
        public bool IsDead => state == PlayerState.Dead;

        private void Awake()
        {
            initialized = ValidateReferences();

            if (!initialized)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged += OnPlayerDamaged;
                playerHealth.Died += OnPlayerDied;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= OnPlayerDamaged;
                playerHealth.Died -= OnPlayerDied;
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            InputIntent intent = playerInput.CurrentInputIntent;

            if (playerHealth.IsDead)
            {
                EnterDeadState();
                animatorBridge.Render(PlayerState.Dead, 0f);
                return;
            }

            playerAim.SetEnabled(true);
            playerAim.Tick(intent.AimWorldPosition);
            playerDodge.TickState(Time.deltaTime);

            playerHealth.SetInvulnerable(
                playerDodge.IsInvulnerable);

            UpdateHurtState();

            if (state == PlayerState.Dodging &&
                playerDodge.IsFinished)
            {
                state = PlayerState.Normal;
            }

            if (state == PlayerState.Normal &&
                intent.DodgePressed &&
                playerDodge.TryStart(
                    intent.Move,
                    playerAim.AimDirection))
            {
                state = PlayerState.Dodging;
            }

            Vector2 move = state == PlayerState.Normal
                ? intent.Move
                : Vector2.zero;

            playerMotor.SetMovementEnabled(true);
            playerMotor.SetMoveIntent(move);

            bool canFire = state == PlayerState.Normal &&
               !playerHealth.IsDead;
            playerWeapon.TickWeapon(intent.FireHeld, canFire);


            animatorBridge.Render(
                state,
                playerMotor.MoveSpeed01);
        }

        private void FixedUpdate()
        {
            if (!initialized || playerHealth.IsDead)
            {
                return;
            }

            playerDodge.TickFixed(Time.fixedDeltaTime);
            playerMotor.TickFixed(Time.fixedDeltaTime);
        }

        private void UpdateHurtState()
        {
            if (state != PlayerState.Hurt)
            {
                return;
            }

            hurtRemaining -= Time.deltaTime;

            if (hurtRemaining <= 0f)
            {
                state = PlayerState.Normal;
            }
        }

        private void OnPlayerDamaged(
            FightArena.Core.Combat.DamageInfo damage)
        {
            if (state == PlayerState.Dead)
            {
                return;
            }

            playerDodge.Cancel();
            state = PlayerState.Hurt;
            hurtRemaining = hurtStateDuration;
            playerMotor.SetMoveIntent(Vector2.zero);
        }

        private void OnPlayerDied()
        {
            EnterDeadState();
        }

        private void EnterDeadState()
        {
            if (state == PlayerState.Dead)
            {
                return;
            }

            state = PlayerState.Dead;
            hurtRemaining = 0f;
            playerDodge.Cancel();
            playerMotor.SetMoveIntent(Vector2.zero);
            playerMotor.SetMovementEnabled(false);
            playerHealth.SetInvulnerable(false);
        }

        private bool ValidateReferences()
        {
            if (playerInput != null &&
                playerMotor != null &&
                playerWeapon != null &&
                playerAim != null &&
                playerDodge != null &&
                playerHealth != null &&
                animatorBridge != null)
            {
                return true;
            }

            Debug.LogError(
                "PlayerController is missing one or more " +
                "player component references.",
                this);

            return false;
        }

        private void OnValidate()
        {
            hurtStateDuration = Mathf.Max(0f, hurtStateDuration);
        }
    }
}
