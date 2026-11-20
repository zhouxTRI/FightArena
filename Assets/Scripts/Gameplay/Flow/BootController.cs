using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FightArena.Gameplay.Flow
{
    public sealed class BootController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private GameObject loadingRoot;

        private bool started;

        private void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            RunLaunchRequest.ClearPending();
            StartCoroutine(EnterMainMenu());
        }

        private IEnumerator EnterMainMenu()
        {
            yield return null;

            if (loadingRoot != null)
            {
                loadingRoot.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(0.25f);
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
