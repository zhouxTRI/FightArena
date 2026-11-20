using FightArena.Gameplay.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace FightArena.UI
{
    public sealed class ResultMenuView : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private bool listenersAttached;

        private void Awake()
        {
            if (gameFlow == null)
            {
                gameFlow = FindObjectOfType<GameFlow>();
            }

            if (gameFlow == null ||
                panelRoot == null ||
                titleText == null ||
                statsText == null ||
                restartButton == null ||
                mainMenuButton == null)
            {
                Debug.LogError(
                    "ResultMenuView is missing required references.",
                    this);
                enabled = false;
                return;
            }

            restartButton.onClick.AddListener(gameFlow.RestartRun);
            mainMenuButton.onClick.AddListener(gameFlow.ReturnToMainMenu);
            listenersAttached = true;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (!listenersAttached)
            {
                return;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(gameFlow.RestartRun);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(
                    gameFlow.ReturnToMainMenu);
            }
        }

        public void SetVisible(
            bool visible,
            GameFlowState state = GameFlowState.Playing,
            GameFlowSnapshot snapshot = default)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            titleText.text = state == GameFlowState.Victory
                ? "胜利"
                : "失败";

            statsText.text =
                $"击杀 {snapshot.KillCount}/{snapshot.KillTarget}\n" +
                $"时间 {snapshot.ElapsedSeconds:0.0}s";
        }
    }
}
