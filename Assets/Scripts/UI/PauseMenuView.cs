using FightArena.Gameplay.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace FightArena.UI
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameFlow gameFlow;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        private bool listenersAttached;

        private void Awake()
        {
            if (gameFlow == null)
            {
                gameFlow = FindObjectOfType<GameFlow>();
            }

            if (gameFlow == null ||
                panelRoot == null ||
                continueButton == null ||
                mainMenuButton == null ||
                quitButton == null)
            {
                Debug.LogError(
                    "PauseMenuView is missing required references.",
                    this);
                enabled = false;
                return;
            }

            continueButton.onClick.AddListener(gameFlow.ResumeGame);
            mainMenuButton.onClick.AddListener(gameFlow.ReturnToMainMenu);
            quitButton.onClick.AddListener(gameFlow.QuitGame);
            listenersAttached = true;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (!listenersAttached)
            {
                return;
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(gameFlow.ResumeGame);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(
                    gameFlow.ReturnToMainMenu);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(gameFlow.QuitGame);
            }
        }

        public void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }
    }
}
