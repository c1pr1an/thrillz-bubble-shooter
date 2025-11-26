using Brain.Core;
using Brain.Gameplay;
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
        // Private Fields
        private StateMachine<GamePhase> _stateMachine;
        private int _matchSeed;

        // Properties
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
            Application.targetFrameRate = 120;

#if UNITY_EDITOR
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
            _matchSeed = PlayerPrefs.GetInt("GameSeed", System.DateTime.Now.Millisecond);
            Random.InitState(_matchSeed);
            Debug.Log($"Starting Bubble Shooter with seed: {MatchSeed}");

            Cameras.Instance.SetCameraByAspectRatio();
            ObjectPooler.Instance.Init();
            GridManager.Instance.InitializeGrid();
            GridGenerator.Instance.GenerateGrid();
            ScoreManager.Instance.Init();
            BallHighlightManager.Instance.Init(GridManager.Instance, TrajectoryPredictor.Instance, GridManager.Instance.BonusBallContainer);
            UIManager.Instance.Init();

            _stateMachine.ChangeState(GamePhase.Playing);
        }

        private void OnPlayingEnter()
        {
            UIManager.Instance.EntryUI.AnimateEntry();
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