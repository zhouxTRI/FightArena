using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedHash =
            Animator.StringToHash("Speed");
        private static readonly int IsDodgingHash =
            Animator.StringToHash("IsDodging");
        private static readonly int IsDeadHash =
            Animator.StringToHash("IsDead");

        [SerializeField] private Animator playerAnimator;

        private void Awake()
        {
            if (playerAnimator == null)
            {
                Debug.LogError(
                    "PlayerAnimatorBridge requires the Neo Animator.",
                    this);
                enabled = false;
            }
        }

        public void Render(PlayerState state, float speed01)
        {
            if (playerAnimator == null)
            {
                return;
            }

            float effectiveSpeed = state == PlayerState.Normal
                ? Mathf.Clamp01(speed01)
                : 0f;

            playerAnimator.SetFloat(
                SpeedHash,
                effectiveSpeed,
                0.05f,
                Time.deltaTime);

            playerAnimator.SetBool(
                IsDodgingHash,
                state == PlayerState.Dodging);

            playerAnimator.SetBool(
                IsDeadHash,
                state == PlayerState.Dead);
        }

        public void ResetForNewSession()
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetFloat(SpeedHash, 0f);
            playerAnimator.SetBool(IsDodgingHash, false);
            playerAnimator.SetBool(IsDeadHash, false);
            playerAnimator.Play("Idle", 0, 0f);
        }
    }
}
