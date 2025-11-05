using System.Collections;
using System.Collections.Generic;
using Brain.Audio;
using Brain.Gameplay;
using Brain.UI;
using Brain.Util;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Brain.Managers
{
    public class ScoreManager : UnitySingleton<ScoreManager>
    {
        // Constants
        private const int TIME_BONUS_PER_SECOND = 200;
        private const int CLEAR_BONUS = 10000;
        private const float SCALE_IN_DURATION = 0.3f;
        private const float HOLD_DURATION = 0.3f;
        private const float SCALE_OUT_DURATION = 0.1f;
        private const int MAX_STREAK = 7;
        private const int BASE_SCORE_PER_BALL = 10;
        private const int BASE_ORPHAN_BONUS = 100;

        // Properties
        [SerializeField] public int ScoreCount { get; private set; }
        [SerializeField] public int BonusScoreCount { get; private set; }
        [SerializeField] public int CurrentStreak { get; private set; } = 1; // Starts at 1 (10 points per ball)
        [SerializeField] public TextMeshPro currentStreakText;

        // Private Fields
        private int _timeBonus = 0;
        private int _clearBonus = 0;
        private List<Tween> _activeAnimations = new List<Tween>();

        // Public Methods
        public void AddBubblePopScore(Vector3 worldPosition, int scoreValue)
        {
            // Get text from pool
            var addScoreText = ObjectPooler.Instance.Get(PooledObjectTag.BallScore_Text).GetComponent<TextMeshPro>();

            addScoreText.transform.SetParent(GridManager.Instance.GridContainer);

            addScoreText.transform.position = worldPosition - 0.05f * Vector3.up;
            addScoreText.transform.localScale = Vector3.zero;
            addScoreText.text = scoreValue.ToString();
            addScoreText.gameObject.SetActive(true);

            // Create animation sequence
            var sequence = DOTween.Sequence();

            // Scale in with bounce
            sequence.Append(addScoreText.transform.DOScale(Vector3.one, SCALE_IN_DURATION).SetEase(Ease.OutBack));

            // Hold
            sequence.AppendInterval(HOLD_DURATION);

            // Scale out
            sequence.Append(addScoreText.transform.DOScale(Vector3.zero, SCALE_OUT_DURATION).SetEase(Ease.InBack));

            // Cleanup
            sequence.OnComplete(() =>
            {
                _activeAnimations.Remove(sequence);
                ObjectPooler.Instance.Release(addScoreText.gameObject, PooledObjectTag.BallScore_Text);
            });

            _activeAnimations.Add(sequence);

            // Update score
            ScoreCount += scoreValue;
            UIManager.Instance.GameplayUI.UpdateScoreText(ScoreCount);

            // Play sound and haptic
            SoundManager.Instance.PlaySfxOneShot(SoundType.Game_ScoreAdd);
            HapticManager.Instance.TriggerHaptic(HapticType.Selection);
        }

        public void AddTimeBonus(int secondsRemaining)
        {
            _timeBonus = secondsRemaining * TIME_BONUS_PER_SECOND;
            ScoreCount += _timeBonus;
            BonusScoreCount += _timeBonus;
            UIManager.Instance.GameplayUI.UpdateScoreText(ScoreCount);
        }

        public void AddClearBonus()
        {
            _clearBonus = CLEAR_BONUS;
            ScoreCount += _clearBonus;
            BonusScoreCount += _clearBonus;
            UIManager.Instance.GameplayUI.UpdateScoreText(ScoreCount);
        }

        public void SetScore(int score)
        {
            ScoreCount = score;
            UIManager.Instance.GameplayUI.UpdateScoreText(ScoreCount);
        }

        public void ProcessScoreUndo(int score)
        {
            SetScore(score);
            StopOngoingAnimations();
        }

        /// <summary>
        /// Get the current score value per ball based on streak
        /// </summary>
        public int GetCurrentBallScore()
        {
            return CurrentStreak * BASE_SCORE_PER_BALL;
        }

        /// <summary>
        /// Get the current score value for orphan balls based on streak
        /// </summary>
        public int GetCurrentOrphanScore()
        {
            return BASE_ORPHAN_BONUS + (CurrentStreak * BASE_SCORE_PER_BALL);
        }

        /// <summary>
        /// Increase streak when balls are destroyed (up to max)
        /// </summary>
        public void IncreaseStreak()
        {
            if (CurrentStreak < MAX_STREAK)
            {
                CurrentStreak++;
                currentStreakText.text = "<size=80%>x</size>" + CurrentStreak.ToString();
            }
        }

        /// <summary>
        /// Reset streak back to 1 when no balls are destroyed
        /// </summary>
        public void ResetStreak()
        {
            CurrentStreak = 1;
            currentStreakText.text = string.Empty;
        }

        /// <summary>
        /// Keep streak the same (used for bonus balls)
        /// </summary>
        public void MaintainStreak()
        {
            // No change to CurrentStreak
        }

        private void StopOngoingAnimations()
        {
            foreach (var tween in _activeAnimations)
            {
                if (tween.IsActive())
                {
                    tween.Kill();
                }
            }
            _activeAnimations.Clear();
        }

        public IEnumerator SaveScore()
        {
            Input.multiTouchEnabled = true;
            int finalScore = ScoreCount;
            int baseScore = ScoreCount - BonusScoreCount;

            Dictionary<string, int> objectives = new Dictionary<string, int>
            {
                { "Base Score", baseScore },
                { "Clear Bonus", _clearBonus },
                { "Time Bonus", _timeBonus }
            };

            // ThrillzSaveScoreData saveScoreData = new ThrillzSaveScoreData
            // {
            //     gameId = 17,
            //     finalScore = finalScore,
            //     objectivesScores = objectives
            // };

            //ThrillzSaveScore.SaveScore(saveScoreData, 0f);
            yield return null;
        }
    }
}
