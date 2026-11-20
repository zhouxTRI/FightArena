using FightArena.Gameplay.Flow;
using FightArena.Gameplay.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FightArena.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerDodge playerDodge;
        [SerializeField] private GameObject hudRoot;

        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Text healthText;

        [Header("Dodge")]
        [SerializeField] private Image dodgeFill;
        [SerializeField] private Text dodgeText;

        [Header("Run")]
        [SerializeField] private Text killText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text timeText;

        [Header("Prompts")]
        [SerializeField] private Text pickupPrompt;
        [SerializeField] private Text toastText;

        [Header("Crosshair")]
        [SerializeField] private RectTransform crosshair;

        private bool subscribed;
        private int lastKillCount = -1;
        private int lastKillTarget = -1;
        private RunMode lastMode = (RunMode)(-1);
        private int lastDisplayedSeconds = -1;
        private int lastHealth = -1;
        private int lastMaxHealth = -1;
        private int lastDodgeTenths = -1;
        private RectTransform crosshairParent;

        private void Awake()
        {
            if (gameFlow == null)
            {
                gameFlow = FindObjectOfType<GameFlow>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }

            if (playerDodge == null)
            {
                playerDodge = FindObjectOfType<PlayerDodge>();
            }

            if (crosshair != null)
            {
                crosshairParent = crosshair.parent as RectTransform;
            }

            if (gameFlow == null ||
                playerHealth == null ||
                playerDodge == null ||
                hudRoot == null ||
                healthFill == null ||
                healthText == null ||
                dodgeFill == null ||
                dodgeText == null ||
                killText == null ||
                modeText == null ||
                timeText == null ||
                crosshair == null ||
                crosshairParent == null)
            {
                Debug.LogError(
                    "HudView is missing required data or UI references.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshSnapshot();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            RefreshHealth();
            RefreshDodge();
            RefreshCrosshair();
        }

        public void SetVisible(bool visible)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(visible);
            }
        }

        public void ShowToast(string message)
        {
            if (toastText != null)
            {
                toastText.text = message ?? string.Empty;
            }
        }

        public void SetPickupPrompt(string message)
        {
            if (pickupPrompt != null)
            {
                pickupPrompt.text = message ?? string.Empty;
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (gameFlow != null)
            {
                gameFlow.SnapshotChanged += OnSnapshotChanged;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (gameFlow != null)
            {
                gameFlow.SnapshotChanged -= OnSnapshotChanged;
            }

            subscribed = false;
        }

        private void RefreshSnapshot()
        {
            if (gameFlow != null)
            {
                OnSnapshotChanged(gameFlow.Snapshot);
            }
        }

        private void OnSnapshotChanged(GameFlowSnapshot snapshot)
        {
            if (killText != null)
            {
                if (lastKillCount != snapshot.KillCount ||
                    lastKillTarget != snapshot.KillTarget)
                {
                    lastKillCount = snapshot.KillCount;
                    lastKillTarget = snapshot.KillTarget;
                    killText.text =
                        $"击杀 {snapshot.KillCount}/{snapshot.KillTarget}";
                }
            }

            if (modeText != null)
            {
                if (lastMode != snapshot.Mode)
                {
                    lastMode = snapshot.Mode;
                    modeText.text = snapshot.Mode == RunMode.Normal
                        ? "普通模式"
                        : $"压力模式 {snapshot.KillTarget}";
                }
            }

            if (timeText != null)
            {
                int totalSeconds =
                    Mathf.FloorToInt(snapshot.ElapsedSeconds);

                if (lastDisplayedSeconds != totalSeconds)
                {
                    lastDisplayedSeconds = totalSeconds;
                    timeText.text =
                        $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
                }
            }
        }

        private void RefreshHealth()
        {
            if (playerHealth == null)
            {
                return;
            }

            float ratio = playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth /
                  playerHealth.MaxHealth
                : 0f;

            if (healthFill != null)
            {
                healthFill.fillAmount = Mathf.Clamp01(ratio);
            }

            if (healthText != null)
            {
                if (lastHealth != playerHealth.CurrentHealth ||
                    lastMaxHealth != playerHealth.MaxHealth)
                {
                    lastHealth = playerHealth.CurrentHealth;
                    lastMaxHealth = playerHealth.MaxHealth;
                    healthText.text =
                        $"{playerHealth.CurrentHealth}/" +
                        $"{playerHealth.MaxHealth}";
                }
            }
        }

        private void RefreshDodge()
        {
            if (playerDodge == null)
            {
                return;
            }

            float cooldownDuration = playerDodge.CooldownDuration;
            float readyRatio = cooldownDuration > 0f
                ? 1f - Mathf.Clamp01(
                playerDodge.CooldownRemaining /
                cooldownDuration)
                : 1f;

            if (dodgeFill != null)
            {
                dodgeFill.fillAmount = readyRatio;
            }

            if (dodgeText != null)
            {
                int tenths = Mathf.CeilToInt(
                    playerDodge.CooldownRemaining * 10f);

                if (lastDodgeTenths != tenths)
                {
                    lastDodgeTenths = tenths;
                    dodgeText.text = tenths <= 0
                        ? "闪避就绪"
                        : $"闪避 {tenths * 0.1f:0.0}s";
                }
            }
        }

        private void RefreshCrosshair()
        {
            bool visible = gameFlow != null &&
                           gameFlow.State == GameFlowState.Playing;

            crosshair.gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    crosshairParent,
                    Input.mousePosition,
                    null,
                    out Vector2 localPoint))
            {
                crosshair.anchoredPosition = localPoint;
            }
        }
    }
}
