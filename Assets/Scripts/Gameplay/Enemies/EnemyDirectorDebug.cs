using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemyDirectorDebug : MonoBehaviour
    {
        [SerializeField] private EnemyDirector director;

        private void Update()
        {
            if (director == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                director.SpawnMelee();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                director.SpawnRanged();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                director.DespawnAll();
            }
        }
    }
}
