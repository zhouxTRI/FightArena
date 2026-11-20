using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FightArena.Gameplay.Flow
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button normalRunButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string arenaSceneName = "Arena";

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            RunLaunchRequest.ClearPending();

            if (normalRunButton == null || quitButton == null)
            {
                Debug.LogError(
                    "MainMenuController requires both menu buttons.",
                    this);
                enabled = false;
                return;
            }

            normalRunButton.onClick.AddListener(StartNormalRun);
            quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDestroy()
        {
            if (normalRunButton != null)
            {
                normalRunButton.onClick.RemoveListener(StartNormalRun);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }
        }

        public void StartNormalRun()
        {
            RunLaunchRequest.RequestNormalRun();
            SceneManager.LoadScene(arenaSceneName);
        }

        public void QuitGame()
        {
            ApplicationQuitUtility.Quit();
        }
    }
}
