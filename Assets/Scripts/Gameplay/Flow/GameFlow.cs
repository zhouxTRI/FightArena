using System;
using System.Collections;
using FightArena.Core.Combat;
using FightArena.Core.Input;
using FightArena.Gameplay.CameraSystem;
using FightArena.Gameplay.Combat;
using FightArena.Gameplay.Enemies;
using FightArena.Gameplay.Player;
using FightArena.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FightArena.Gameplay.Flow
{
    public sealed class GameFlow : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private EnemyDirector enemyDirector;
        [SerializeField] private ProjectileService projectileService;
        [SerializeField] private CameraFollow2D cameraFollow;

        [Header("Views")]
        [SerializeField] private HudView hudView;
        [SerializeField] private PauseMenuView pauseMenuView;
        [SerializeField] private ResultMenuView resultMenuView;

        [Header("Rules")]
        [SerializeField, Min(1)] private int normalKillTarget = 45;
        [SerializeField, Min(0.01f)] private float snapshotPublishInterval = 0.1f;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string arenaSceneName = "Arena";

        private GameFlowState state = GameFlowState.Boot;
        private RunMode mode;
        private int killCount;
        private int killTarget;
        private float elapsedSeconds;
        private float lockedTimeScale = 1f;
        private float snapshotTimer;
        private bool initialized;
        private bool subscribed;

        public GameFlowState State => state;
        public RunMode Mode => mode;
        public int KillCount => killCount;
        public int KillTarget => killTarget;
        public float ElapsedSeconds => elapsedSeconds;
        public GameFlowSnapshot Snapshot => BuildSnapshot();

        public event Action<GameFlowState, GameFlowState> StateChanged;
        public event Action<GameFlowSnapshot> SnapshotChanged;

        private void Awake()
        {
            ResolveSceneReferences();

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (RunLaunchRequest.Consume(
                    out RunMode requestedMode,
                    out int requestedStressCount))
            {
                mode = requestedMode;
                killTarget = mode == RunMode.Normal
                    ? normalKillTarget
                    : Mathf.Max(1, requestedStressCount);
            }
            else
            {
                mode = RunMode.Normal;
                killTarget = normalKillTarget;
            }

            Time.timeScale = 1f;
            initialized = true;
            Subscribe();
            EnterState(GameFlowState.Playing);
        }

        private void ResolveSceneReferences()
        {
            if (inputRouter == null)
            {
                inputRouter = FindObjectOfType<InputRouter>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }

            if (enemyDirector == null)
            {
                enemyDirector = FindObjectOfType<EnemyDirector>();
            }

            if (projectileService == null)
            {
                projectileService = FindObjectOfType<ProjectileService>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindObjectOfType<CameraFollow2D>();
            }

            if (hudView == null)
            {
                hudView = FindObjectOfType<HudView>(true);
            }

            if (pauseMenuView == null)
            {
                pauseMenuView = FindObjectOfType<PauseMenuView>(true);
            }

            if (resultMenuView == null)
            {
                resultMenuView = FindObjectOfType<ResultMenuView>(true);
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (state == GameFlowState.Playing)
            {
                elapsedSeconds += Time.deltaTime;
                snapshotTimer += Time.deltaTime;

                if (snapshotTimer >= snapshotPublishInterval)
                {
                    snapshotTimer = 0f;
                    PublishSnapshot();
                }

                if (inputRouter.CurrentInputIntent.PausePressed)
                {
                    PauseGame();
                }
            }
            else if (state == GameFlowState.Paused &&
                     inputRouter.CurrentInputIntent.PausePressed)
            {
                ResumeGame();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void PauseGame()
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            lockedTimeScale = Time.timeScale > 0f
                ? Time.timeScale
                : 1f;

            inputRouter.AddLock(InputLockFlags.Pause);
            Time.timeScale = 0f;
            EnterState(GameFlowState.Paused);
        }

        public void ResumeGame()
        {
            if (state != GameFlowState.Paused)
            {
                return;
            }

            Time.timeScale = lockedTimeScale;
            EnterState(GameFlowState.Playing);
            StartCoroutine(ReleaseInputLockNextFrame(InputLockFlags.Pause));
        }

        public void PauseForInventory()
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            lockedTimeScale = Time.timeScale > 0f
                ? Time.timeScale
                : 1f;

            inputRouter.AddLock(InputLockFlags.Inventory);
            Time.timeScale = 0f;
            EnterState(GameFlowState.InventoryPaused);
        }

        public void ResumeFromInventory()
        {
            if (state != GameFlowState.InventoryPaused)
            {
                return;
            }

            Time.timeScale = lockedTimeScale;
            EnterState(GameFlowState.Playing);
            StartCoroutine(
                ReleaseInputLockNextFrame(InputLockFlags.Inventory));
        }

        public void ToggleInventoryPause()
        {
            if (state == GameFlowState.Playing)
            {
                PauseForInventory();
            }
            else if (state == GameFlowState.InventoryPaused)
            {
                ResumeFromInventory();
            }
        }

        public void RestartRun()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(arenaSceneName);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void QuitGame()
        {
            ApplicationQuitUtility.Quit();
        }

        private void OnPlayerDamaged(DamageInfo damage)
        {
            cameraFollow?.AddTrauma(0.18f);
        }

        private void OnPlayerDied()
        {
            if (state == GameFlowState.Playing)
            {
                FinishRun(GameFlowState.Defeat);
            }
        }

        private void OnEnemyKilled(EnemyKilledInfo info)
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            killCount++;

            if (killCount >= killTarget)
            {
                FinishRun(GameFlowState.Victory);
            }
            else
            {
                PublishSnapshot();
            }
        }

        private void OnProjectileHit(ProjectileHitResult result)
        {
            if (state == GameFlowState.Playing &&
                result.DamageApplied &&
                !result.HitWall)
            {
                cameraFollow?.AddTrauma(0.08f);
            }
        }

        private void FinishRun(GameFlowState resultState)
        {
            if (state != GameFlowState.Playing)
            {
                return;
            }

            inputRouter.AddLock(
                resultState == GameFlowState.Victory
                    ? InputLockFlags.Victory
                    : InputLockFlags.Defeat);

            Time.timeScale = 0f;
            EnterState(resultState);
        }

        private void EnterState(GameFlowState nextState)
        {
            GameFlowState previousState = state;
            state = nextState;
            StateChanged?.Invoke(previousState, nextState);

            hudView?.SetVisible(nextState == GameFlowState.Playing);
            pauseMenuView?.SetVisible(nextState == GameFlowState.Paused);
            resultMenuView?.SetVisible(
                nextState == GameFlowState.Victory ||
                nextState == GameFlowState.Defeat,
                nextState,
                BuildSnapshot());

            bool gameplayActive = nextState == GameFlowState.Playing;
            Cursor.visible = !gameplayActive;
            Cursor.lockState = gameplayActive
                ? CursorLockMode.Confined
                : CursorLockMode.None;

            PublishSnapshot();
        }

        private GameFlowSnapshot BuildSnapshot()
        {
            return new GameFlowSnapshot(
                state,
                mode,
                killCount,
                killTarget,
                elapsedSeconds);
        }

        private void PublishSnapshot()
        {
            SnapshotChanged?.Invoke(BuildSnapshot());
        }

        private IEnumerator ReleaseInputLockNextFrame(
            InputLockFlags lockFlag)
        {
            yield return new WaitForEndOfFrame();
            inputRouter.RemoveLock(lockFlag);
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            playerHealth.Damaged += OnPlayerDamaged;
            playerHealth.Died += OnPlayerDied;
            enemyDirector.EnemyKilled += OnEnemyKilled;

            if (projectileService != null)
            {
                projectileService.ProjectileHit += OnProjectileHit;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (playerHealth != null)
            {
                playerHealth.Damaged -= OnPlayerDamaged;
                playerHealth.Died -= OnPlayerDied;
            }

            if (enemyDirector != null)
            {
                enemyDirector.EnemyKilled -= OnEnemyKilled;
            }

            if (projectileService != null)
            {
                projectileService.ProjectileHit -= OnProjectileHit;
            }

            subscribed = false;
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            if (inputRouter == null)
            {
                Debug.LogError("GameFlow is missing InputRouter.", this);
                valid = false;
            }

            if (playerHealth == null)
            {
                Debug.LogError("GameFlow is missing PlayerHealth.", this);
                valid = false;
            }

            if (enemyDirector == null)
            {
                Debug.LogError("GameFlow is missing EnemyDirector.", this);
                valid = false;
            }

            if (hudView == null)
            {
                Debug.LogError("GameFlow is missing HudView.", this);
                valid = false;
            }

            if (pauseMenuView == null)
            {
                Debug.LogError("GameFlow is missing PauseMenuView.", this);
                valid = false;
            }

            if (resultMenuView == null)
            {
                Debug.LogError("GameFlow is missing ResultMenuView.", this);
                valid = false;
            }

            return valid;
        }

        private void OnValidate()
        {
            normalKillTarget = Mathf.Max(1, normalKillTarget);
            snapshotPublishInterval =
                Mathf.Max(0.01f, snapshotPublishInterval);
        }
    }
}
