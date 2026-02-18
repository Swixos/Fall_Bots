using UnityEngine;
using UnityEngine.SceneManagement;
using FallBots.Player;
using FallBots.CourseGeneration;
using FallBots.UI;

namespace FallBots.Managers
{
    /// <summary>
    /// Core game manager. Handles game state, countdown, race logic, respawn, and finish detection.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private float countdownDuration = 3f;
        [SerializeField] private float respawnHeight = -10f;

        [Header("References")]
        [SerializeField] private ProceduralCourseGenerator courseGenerator;
        [SerializeField] private PlayerController player;
        [SerializeField] private GameUI gameUI;

        [Header("Course Seed")]
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 12345;

        private GameState currentState = GameState.Waiting;
        private float countdownTimer;
        private float raceTimer;
        private int lastCheckpointIndex;
        private float bestTime;

        public GameState CurrentState => currentState;
        public float RaceTimer => raceTimer;
        public float BestTime => bestTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            bestTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);
            StartNewRace();
        }

        private void Update()
        {
            switch (currentState)
            {
                case GameState.Countdown:
                    UpdateCountdown();
                    break;

                case GameState.Racing:
                    UpdateRacing();
                    break;

                case GameState.Finished:
                    UpdateFinished();
                    break;
            }
        }

        public void StartNewRace()
        {
            currentState = GameState.Waiting;

            // Generate course
            int seed = useRandomSeed ? Random.Range(0, 999999) : fixedSeed;
            courseGenerator.GenerateCourse(seed);

            // Position player at start
            if (player != null)
            {
                player.HasFinished = false;
                player.Respawn(courseGenerator.StartPosition);
            }

            lastCheckpointIndex = 0;

            // Start countdown
            countdownTimer = countdownDuration;
            currentState = GameState.Countdown;
            raceTimer = 0f;

            if (gameUI != null)
            {
                gameUI.ShowCountdown(true);
                gameUI.ShowFinishScreen(false);
                gameUI.UpdateSeedDisplay(seed);
            }

            // Freeze player during countdown
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }
        }

        private void UpdateCountdown()
        {
            countdownTimer -= Time.deltaTime;

            if (gameUI != null)
            {
                int displayNumber = Mathf.CeilToInt(countdownTimer);
                if (countdownTimer <= 0)
                    gameUI.UpdateCountdown("GO!");
                else
                    gameUI.UpdateCountdown(displayNumber.ToString());
            }

            if (countdownTimer <= 0)
            {
                currentState = GameState.Racing;

                // Unfreeze player
                if (player != null)
                {
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = false;
                }

                if (gameUI != null)
                {
                    gameUI.ShowCountdown(false);
                }
            }
        }

        private void UpdateRacing()
        {
            raceTimer += Time.deltaTime;

            if (gameUI != null)
            {
                gameUI.UpdateTimer(raceTimer);
            }

            // Check for fall respawn
            if (player != null && player.transform.position.y < respawnHeight)
            {
                RespawnAtCheckpoint();
            }
        }

        private void UpdateFinished()
        {
            // Wait for restart input
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartNewRace();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        public void OnPlayerFinished()
        {
            if (currentState != GameState.Racing) return;

            currentState = GameState.Finished;
            player.HasFinished = true;

            bool isNewBest = raceTimer < bestTime;
            if (isNewBest)
            {
                bestTime = raceTimer;
                PlayerPrefs.SetFloat("BestTime", bestTime);
                PlayerPrefs.Save();
            }

            if (gameUI != null)
            {
                gameUI.ShowFinishScreen(true, raceTimer, bestTime, isNewBest);
            }

            // Free cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OnCheckpointReached(int checkpointIndex)
        {
            if (checkpointIndex > lastCheckpointIndex)
            {
                lastCheckpointIndex = checkpointIndex;
                if (gameUI != null)
                {
                    gameUI.ShowCheckpointMessage();
                }
            }
        }

        private void RespawnAtCheckpoint()
        {
            if (courseGenerator.Checkpoints.Count > 0 && lastCheckpointIndex < courseGenerator.Checkpoints.Count)
            {
                player.Respawn(courseGenerator.Checkpoints[lastCheckpointIndex]);
            }
            else
            {
                player.Respawn(courseGenerator.StartPosition);
            }

            // Penalty
            raceTimer += 2f;

            if (gameUI != null)
            {
                gameUI.ShowRespawnPenalty();
            }
        }

        public void RestartRace()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            StartNewRace();
        }
    }

    public enum GameState
    {
        Waiting,
        Countdown,
        Racing,
        Finished
    }
}
