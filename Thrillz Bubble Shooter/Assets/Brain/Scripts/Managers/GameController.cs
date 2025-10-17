using Brain.Core;
using Brain.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Brain.Managers
{
    public enum GamePhase
    {
        Initializing,
        Playing,
    }

    public class GameController : UnitySingleton<GameController>
    {
        private StateMachine<GamePhase> _stateMachine;
        private int _matchSeed;
        public int MatchSeed => _matchSeed;
        public GamePhase CurrentPhase => _stateMachine.CurrentPhase;

        private new void Awake()
        {
            base.Awake();
            _stateMachine = new StateMachine<GamePhase>("Game State Machine");
            InitializeStateMachine();
        }

        private void Start()
        {
            ConfigureApplication();
        }

        private void ConfigureApplication()
        {
            Input.multiTouchEnabled = false;

#if UNITY_EDITOR
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
#endif
        }

        private void InitializeStateMachine()
        {
            _stateMachine.AddState(new State<GamePhase>(GamePhase.Initializing, OnInitializingEnter, null));
            _stateMachine.AddState(new State<GamePhase>(GamePhase.Playing, OnPlayingEnter, null));

            _stateMachine.ChangeState(GamePhase.Initializing);
        }

        private void OnInitializingEnter()
        {
            _matchSeed = PlayerPrefs.GetInt("GameSeed", System.Environment.TickCount);
            Debug.Log($"Starting Bubble Shooter with seed: {MatchSeed}");

            // Initialize grid structure
            GridManager.Instance.InitializeGrid();

            // Generate procedural level (includes neighbor updates and orphan removal)
            LevelGenerator.Instance.GenerateLevel(_matchSeed);

            // Subscribe to game events
            GridScrollManager.Instance.OnDeathLineTouched += OnDeathLineTouched;
            GameConditionsManager.Instance.OnGameWon += OnGameWon;
            GameConditionsManager.Instance.OnGameLost += OnGameLost;

            _stateMachine.ChangeState(GamePhase.Playing);
        }

        private void OnPlayingEnter()
        {
            GameConditionsManager.Instance.StartGame();
            Debug.Log("Game started!");
        }

        private void OnDeathLineTouched()
        {
            GameConditionsManager.Instance.TriggerLose();
        }

        private void OnGameWon()
        {
            Debug.Log("You Win!");
        }

        private void OnGameLost()
        {
            Debug.Log("Game Over!");
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Debug controls
            if (Input.GetKeyDown(KeyCode.P)) Time.timeScale = 0f;
            if (Input.GetKeyDown(KeyCode.O)) Time.timeScale = 1f;
            if (Input.GetKeyDown(KeyCode.I)) Time.timeScale = 0.3f;
            if (Input.GetKey(KeyCode.Escape)) RestartGame();
        }
#endif
    }
}