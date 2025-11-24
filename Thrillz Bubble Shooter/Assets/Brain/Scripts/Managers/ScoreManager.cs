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
        private const int BASE_SCORE_PER_BALL = 10;
        private const int BASE_ORPHAN_BONUS = 100;

        // Properties
        [SerializeField] public int ScoreCount { get; private set; }
        [SerializeField] public int BonusScoreCount { get; private set; }
        [SerializeField] public int CurrentStreak { get; private set; }
        [SerializeField] public TextMeshPro currentStreakText;

        // Private Fields
        private int _timeBonus = 0;
        private int _clearBonus = 0;
        private List<Tween> _activeAnimations = new List<Tween>();

        public void Init()
        {
            ScoreCount = 0;
            BonusScoreCount = 0;
            CurrentStreak = 0;
            currentStreakText.text = string.Empty;
            currentStreakText.transform.localScale = Vector3.zero;
        }

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
            SoundManager.Instance.PlaySfxOneShot(SoundType.Game_MatchPop);
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
        /// Get the score value for bonus balls (ensures minimum base score)
        /// </summary>
        public int GetBonusBallScore()
        {
            // Bonus balls always give at least the base score, even with 0 streak
            return Mathf.Max(BASE_SCORE_PER_BALL, CurrentStreak * BASE_SCORE_PER_BALL);
        }

        public void AddStreakPopup(Vector3 worldPosition, int streakValue)
        {
            // Get text from pool
            var streakText = ObjectPooler.Instance.Get(PooledObjectTag.Combo_Text);

            //Replace 0 with o because of the font
            string text = "<size=80%>x</size>" + streakValue.ToString().Replace('0', 'o');

            streakText.GetComponentInChildren<TextMeshPro>().text = text;

            streakText.transform.SetParent(GridManager.Instance.GridContainer);

            // Calculate target position
            Vector3 targetPos = worldPosition + 3f * Vector3.up;

            // Get screen boundaries
            Camera cam = Cameras.Instance.MainCam;
            float screenAspect = (float)Screen.width / Screen.height;
            float cameraHeight = cam.orthographicSize * 2;
            float cameraWidth = cameraHeight * screenAspect;

            float minX = cam.transform.position.x - (cameraWidth / 2f) + 2.5f; // Increased padding
            float maxX = cam.transform.position.x + (cameraWidth / 2f) - 2.5f; // Increased padding

            // Get top UI boundary
            float topBoundaryY = UIManager.Instance.GameplayUI.GetTopBoundaryY() - 1f; // Add padding

            // Clamp position
            float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
            float clampedY = Mathf.Min(targetPos.y, topBoundaryY);

            streakText.transform.position = new Vector3(clampedX, clampedY, targetPos.z);
            streakText.transform.localScale = Vector3.zero;

            streakText.gameObject.SetActive(true);

            // Create animation sequence
            var sequence = DOTween.Sequence();

            // Scale in with bounce
            sequence.Append(streakText.transform.DOScale(Vector3.one, SCALE_IN_DURATION).SetEase(Ease.OutBack));

            // Hold
            sequence.AppendInterval(HOLD_DURATION);

            // Scale out
            sequence.Append(streakText.transform.DOScale(Vector3.zero, SCALE_OUT_DURATION).SetEase(Ease.InBack));

            // Cleanup
            sequence.OnComplete(() =>
            {
                _activeAnimations.Remove(sequence);
                ObjectPooler.Instance.Release(streakText.gameObject, PooledObjectTag.Combo_Text);
            });

            _activeAnimations.Add(sequence);
        }

        /// <summary>
        /// Increase streak when balls are destroyed (up to max)
        /// </summary>
        public void IncreaseStreak(Vector3 worldPosition)
        {
            CurrentStreak++;
            UpdateStreakUI();

            if (CurrentStreak > 1)
            {
                AddStreakPopup(worldPosition, CurrentStreak);
            }
        }

        public void PlayStreakSound()
        {
            if (CurrentStreak >= 2)
            {
                float pitch = 1f + ((CurrentStreak - 2) * 0.1f);
                pitch = Mathf.Clamp(pitch, 1f, 1.5f);
                SoundManager.Instance.PlaySfxOneShot(SoundType.Game_Streak, pitch);
            }
        }

        /// <summary>
        /// Reset streak back to 1 when no balls are destroyed
        /// </summary>
        public void ResetStreak()
        {
            CurrentStreak = 0;
            HideStreakUI();
        }

        private void UpdateStreakUI()
        {
            if (CurrentStreak <= 1)
            {
                HideStreakUI();
                return;
            }

            string newText = "<size=80%>x</size>" + CurrentStreak.ToString().Replace('0', 'o');
            bool wasEmpty = string.IsNullOrEmpty(currentStreakText.text);

            currentStreakText.text = newText;

            if (wasEmpty)
            {
                // Scale-in animation when appearing
                currentStreakText.transform.localScale = Vector3.zero;
                currentStreakText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
            else
            {
                // Punch animation when updating
                currentStreakText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);
            }
        }

        private void HideStreakUI()
        {
            if (string.IsNullOrEmpty(currentStreakText.text))
                return;

            // Scale-down animation when disappearing
            currentStreakText.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                currentStreakText.text = string.Empty;
            });
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
