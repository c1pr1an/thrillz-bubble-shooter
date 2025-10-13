using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brain.Audio;
using Brain.UI;
using Brain.Util;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Brain.Managers
{
    public enum ScoreType
    {
        Wild,
        Blackjack21,
        FiveCards,
        FiveCardsAt21,
        GreatStreak,
        SuperStreak,
        MegaStreak,
        MonsterStreak,
        CrazyStreak,
        MassiveStreak,
        NoBustBonus = 100,
        PerfectGameBonus,
        LivesBonus,
        TimeBonus,
        None
    }

    [System.Serializable]
    public struct ScoreData
    {
        public ScoreType scoreType;
        public int scoreValue;
        public GameObject scorePrefab;
        public bool isBonusScore;
    }

    public class ScoreManager : UnitySingleton<ScoreManager>
    {
        public const float SCORE_TEXT_DELAY = 1.4f;

        [SerializeField] public int ScoreCount { get; private set; }
        [SerializeField] public int StreakCount { get; private set; }
        [SerializeField] public int BonusScoreCount { get; private set; }

        [SerializeField] private GameObject _scoreHighlightVFXPrefab;
        [SerializeField] private List<ScoreData> scoreDataList;

        private List<ActiveScoreAnimation> _activeAnimations = new List<ActiveScoreAnimation>();
        private List<Tween> _delayedCalls = new List<Tween>();
        private bool _bonusScoresAdded = false;

        private class ActiveScoreAnimation
        {
            public TextMeshProUGUI AddScoreText;
            public int ScoreToAdd;
            public int CurrentScore;
            public bool IsCancelled;
        }

        public void AddScore(ScoreType scoreType, Vector3 canvasPos, bool increaseStreak)
        {
            ScoreData scoreData = GetScoreData(scoreType);

            if (scoreData.scorePrefab != null)
            {
                SoundManager.Instance.PlaySfxOneShot(SoundType.Game_ScoreAppear);
                ScoreTextFx(scoreData.scorePrefab, canvasPos, scoreData.scoreValue, ScoreCount, false, scoreData.isBonusScore);
            }
        }

        public void ScoreTextFx(GameObject specialScorePrefab, Vector3 position, int scoreToAdd, int currentScore, bool isStreakScore, bool isBonusScore)
        {
            var gameplayUITransform = UIManager.Instance.GameplayUI.transform;
            var addScoreText = CreateAddScoreText(scoreToAdd);

            var anim = new ActiveScoreAnimation
            {
                AddScoreText = addScoreText,
                ScoreToAdd = scoreToAdd,
                CurrentScore = currentScore,
                IsCancelled = false
            };


            AnimateAddScoreFinalEffects(addScoreText, scoreToAdd, anim, isStreakScore, isBonusScore);

        }

        private TextMeshProUGUI CreateAddScoreText(int scoreToAdd)
        {
            var addScoreText = ObjectPooler.Instance.Get(PooledObjectTag.AddScoreText).GetComponent<TextMeshProUGUI>();
            addScoreText.transform.SetParent(UIManager.Instance.transform);
            addScoreText.transform.position = Vector3.zero;
            addScoreText.transform.rotation = Quaternion.identity;
            addScoreText.text = "+" + scoreToAdd;
            addScoreText.gameObject.SetActive(true);
            return addScoreText;
        }

        private void AnimateAddScoreFinalEffects(TextMeshProUGUI addScoreText, int scoreToAdd, ActiveScoreAnimation anim, bool isStreakScore, bool isBonusScore)
        {
            addScoreText.transform.DOMove(UIManager.Instance.GameplayUI.ScoreText.transform.position, 0.35f)
                .SetEase(Ease.InBack)
                .SetDelay(0.8f)
                .OnComplete(() =>
                {
                    _activeAnimations.Remove(anim);

                    if (!anim.IsCancelled)
                    {
                        var scoreTransform = UIManager.Instance.GameplayUI.ScoreText.transform;
                        DOTween.Kill(scoreTransform);
                        scoreTransform.localScale = Vector3.one;
                        scoreTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1f);

                        ScoreCount += scoreToAdd;
                        if (isBonusScore) BonusScoreCount += scoreToAdd;

                        UIManager.Instance.GameplayUI.UpdateScoreText(ScoreCount);

                        SoundManager.Instance.PlaySfxOneShot(SoundType.Game_ScoreAdd);
                        HapticManager.Instance.TriggerHaptic(HapticType.Selection);
                    }

                    ObjectPooler.Instance.Release(addScoreText.gameObject, PooledObjectTag.AddScoreText);
                });
        }

        public float AddBonusScores(List<ScoreType> scoreTypes, bool animated)
        {
            if (_bonusScoresAdded) return 0f;
            _bonusScoresAdded = true;

            float finishDelay = SCORE_TEXT_DELAY;

            if (StreakCount >= 2)
                finishDelay += SCORE_TEXT_DELAY;

            Vector2 baseCanvasPos = RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);

            int totalBonuses = scoreTypes.Count;
            for (int i = 0; i < totalBonuses; i++)
            {
                Vector2 offsetPos = GetBonusScoreOffsetPosition(baseCanvasPos, i, totalBonuses);

                ScoreType scoreType = scoreTypes[i];
                if (animated)
                {
                    DOVirtual.DelayedCall(finishDelay, () =>
                    {
                        AddScore(scoreType, offsetPos, false);
                    });
                }
                else
                {
                    ScoreData scoreData = GetScoreData(scoreType);

                    ScoreCount += scoreData.scoreValue;
                    if (scoreData.isBonusScore)
                    {
                        BonusScoreCount += scoreData.scoreValue;
                    }
                }

                finishDelay += SCORE_TEXT_DELAY / 3f;
            }

            return finishDelay + SCORE_TEXT_DELAY;
        }

        private Vector2 GetBonusScoreOffsetPosition(Vector2 baseCanvasPos, int index, int totalBonuses)
        {
            const float horizontalSpacing = 400f;
            float totalWidth = (totalBonuses - 1) * horizontalSpacing;
            float startX = baseCanvasPos.x - totalWidth / 2f;

            float offsetX = startX + index * horizontalSpacing;
            return new Vector2(offsetX, baseCanvasPos.y);
        }

        public void SetScore(int score)
        {
            ScoreCount = score;
            UIManager.Instance.GameplayUI.UpdateScoreText(ScoreCount);
        }

        public void SetStreak(int streak)
        {
            StreakCount = streak;
        }

        public void ResetStreak()
        {
            SetStreak(0);
        }

        public void ProcessScoreUndo(int score)
        {
            SetScore(score);
            StopOngoingAnimations();
        }

        private void StopOngoingAnimations()
        {
            foreach (Tween t in _delayedCalls)
            {
                if (t.IsActive()) t.Kill();
            }
            _delayedCalls.Clear();

            foreach (var anim in _activeAnimations)
            {
                anim.IsCancelled = true;

                if (anim.AddScoreText != null)
                {
                    DOTween.Kill(anim.AddScoreText.transform);
                    anim.AddScoreText.gameObject.SetActive(false);
                    ObjectPooler.Instance.Release(anim.AddScoreText.gameObject, PooledObjectTag.AddScoreText);
                }
            }
            _activeAnimations.Clear();
        }

        private ScoreData GetScoreData(ScoreType scoreType)
        {
            ScoreData data = new ScoreData { scoreValue = 0, scorePrefab = null };
            foreach (ScoreData s in scoreDataList)
            {
                if (s.scoreType == scoreType)
                {
                    data = s;
                    break;
                }
            }

            if (data.scoreType == ScoreType.LivesBonus)
            {
                //data.scoreValue *= GameController.Instance.LivesAmount;
            }
            else if (data.scoreType == ScoreType.TimeBonus)
            {
                //data.scoreValue *= GameController.Instance.GameTimer;
            }

            return data;
        }

        public IEnumerator SaveScore()
        {
            Input.multiTouchEnabled = true;
            int timeScoreMultiplier = scoreDataList.FirstOrDefault(s => s.scoreType == ScoreType.TimeBonus).scoreValue;
            int timeScore = 0; // GameManager.Instance.GameTimer * timeScoreMultiplier;
            int finalScore = ScoreCount;
            int streakNbonus = BonusScoreCount - timeScore; //Time score is added to BonusScoreCount, so we subtract it here
            int baseScore = ScoreCount - BonusScoreCount;

            Dictionary<string, int> objectives = new Dictionary<string, int>
            {
                { "Base Score", baseScore },
                { "Streak & Bonus", streakNbonus },
                { "Time Bonus", timeScore }
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
