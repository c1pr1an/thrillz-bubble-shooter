using System;
using System.Collections;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class GameConditionsManager : UnitySingleton<GameConditionsManager>
    {
        // Serialized Fields
        [Header("Game Settings")]
        [SerializeField] private float _gameDuration = 120f;

        // Private Fields
        private float _timeRemaining;
        private bool _gameActive = false;

        // Properties
        public float TimeRemaining => _timeRemaining;
        public bool IsGameActive => _gameActive;

        public event Action<float> OnTimerUpdated;

        // Public Methods
        public void StartGame()
        {
            _timeRemaining = _gameDuration;
            _gameActive = true;
            StartCoroutine(GameTimer());
        }

        public void StopGame()
        {
            _gameActive = false;
            StopAllCoroutines();
        }

        // Private Methods - Coroutine for game timer
        private IEnumerator GameTimer()
        {
            while (_timeRemaining > 0 && _gameActive)
            {
                _timeRemaining -= Time.deltaTime;
                OnTimerUpdated?.Invoke(_timeRemaining);

                if (_timeRemaining <= 0)
                {
                    TriggerLose();
                }

                yield return null;
            }
        }

        public void TriggerWin()
        {
            if (!_gameActive) return;

            _gameActive = false;
            StopAllCoroutines();

            UIManager.Instance.GameFinishedPanel.Display(1f);
        }

        public void TriggerLose()
        {
            if (!_gameActive) return;

            _gameActive = false;
            StopAllCoroutines();

            UIManager.Instance.OutOfTimePanel.Display();
        }

        public void ResetGame()
        {
            StopGame();
            _timeRemaining = _gameDuration;
        }
    }
}
