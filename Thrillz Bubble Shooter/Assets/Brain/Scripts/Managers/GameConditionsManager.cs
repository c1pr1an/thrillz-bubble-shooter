using System;
using System.Collections;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;
using DG.Tweening;
using Brain.Audio;

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

        public void TriggerLimitHit(Ball ball)
        {
            if (!_gameActive) return;

            _gameActive = false;
            StopAllCoroutines();

            GridManager gm = GridManager.Instance;

            float limitLineRedYScale = gm.LimitLineRed.transform.localScale.y;
            gm.LimitLineRed.transform.localScale = new Vector3(gm.LimitLineRed.transform.localScale.x, 0f, gm.LimitLineRed.transform.localScale.z);
            gm.LimitLineRed.transform.DOScaleY(limitLineRedYScale, 0.2f).SetEase(Ease.OutBack);
            gm.LimitLineRed.gameObject.SetActive(true);

            gm.LimitHitBallVFX.transform.position = ball.transform.position;
            gm.LimitHitBallVFX.transform.localScale = Vector3.zero;
            gm.LimitHitBallVFX.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            gm.LimitHitBallVFX.gameObject.SetActive(true);
            gm.LimitHitBallVFX.Play();

            SoundManager.Instance.PlaySfxOneShot(SoundType.UI_LimitHit);
            UIManager.Instance.LimitHitPanel.Display();
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
