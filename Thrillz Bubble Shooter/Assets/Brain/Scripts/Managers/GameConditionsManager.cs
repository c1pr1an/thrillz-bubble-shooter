using System;
using System.Collections;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class GameConditionsManager : UnitySingleton<GameConditionsManager>
    {
        [Header("Game Settings")]
        [SerializeField] private float gameDuration = 120f;

        private float timeRemaining;
        private bool gameActive = false;

        public float TimeRemaining => timeRemaining;
        public bool IsGameActive => gameActive;

        public event Action OnGameWon;
        public event Action OnGameLost;
        public event Action<float> OnTimerUpdated;

        /// <summary>
        /// Starts the game timer
        /// </summary>
        public void StartGame()
        {
            timeRemaining = gameDuration;
            gameActive = true;
            StartCoroutine(GameTimer());
        }

        /// <summary>
        /// Stops the game
        /// </summary>
        public void StopGame()
        {
            gameActive = false;
            StopAllCoroutines();
        }

        /// <summary>
        /// Game timer coroutine
        /// </summary>
        private IEnumerator GameTimer()
        {
            while (timeRemaining > 0 && gameActive)
            {
                timeRemaining -= Time.deltaTime;
                OnTimerUpdated?.Invoke(timeRemaining);

                if (timeRemaining <= 0)
                {
                    TriggerLose();
                }

                yield return null;
            }
        }

        /// <summary>
        /// Checks win condition
        /// </summary>
        public void CheckWinCondition()
        {
            if (!gameActive) return;

            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.Balls == null) return;

            bool anyBallsLeft = false;

            foreach (var row in gridManager.Balls)
            {
                foreach (var ball in row)
                {
                    if (ball != null)
                    {
                        anyBallsLeft = true;
                        break;
                    }
                }
                if (anyBallsLeft) break;
            }

            if (!anyBallsLeft)
            {
                TriggerWin();
            }
        }

        /// <summary>
        /// Triggers win state
        /// </summary>
        public void TriggerWin()
        {
            if (!gameActive) return;

            gameActive = false;
            StopAllCoroutines();
            OnGameWon?.Invoke();
        }

        /// <summary>
        /// Triggers lose state
        /// </summary>
        public void TriggerLose()
        {
            if (!gameActive) return;

            gameActive = false;
            StopAllCoroutines();
            OnGameLost?.Invoke();
        }

        /// <summary>
        /// Resets game conditions
        /// </summary>
        public void ResetGame()
        {
            StopGame();
            timeRemaining = gameDuration;
        }
    }
}
