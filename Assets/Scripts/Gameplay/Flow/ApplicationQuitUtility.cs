using UnityEngine;

namespace FightArena.Gameplay.Flow
{
    public static class ApplicationQuitUtility
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Time.timeScale = 1f;
            Application.Quit();
#endif
        }
    }
}
