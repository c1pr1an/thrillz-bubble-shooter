using System.Collections;
using System.Collections.Generic;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brain.UI
{
    public class GameplayUI : MonoBehaviour
    {
        public List<Image> LivesImages;
        public Sprite EmptyLifeSprite;
        public Sprite FullLifeSprite;
        [SerializeField] private GameObject _oneMinuteLeftPanel;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        public TextMeshProUGUI ScoreText
        {
            get { return _scoreText; }
        }

        private bool _oneMinuteLeftShown = false;
        private bool _isTimeTextPulsing = false;

        public void Display()
        {
            LevelManager.Instance.LevelInstance.RevealLevel();
        }

        public void PauseOnClick()
        {
            //UIManager.Instance.GlobalPauseMode.TriggerPause();
        }

        public void UpdateScoreText(int score)
        {
            _scoreText.text = score.ToString();
        }

        public void SetGameTime(int seconds)
        {
            if (seconds < 0) seconds = 0;

            int sec = seconds % 60;
            int min = (seconds % 3600) / 60;
            _timerText.text = string.Format("{0,2}:{1,2}", min.ToString().PadLeft(2, '0'), sec.ToString().PadLeft(2, '0'));

            if (min == 0 && seconds <= 20 && !_isTimeTextPulsing)
            {
                _isTimeTextPulsing = true;

                Color targetColor = new Color32(0xFF, 0x7F, 0x8E, 0xFF);
                _timerText.color = Color.white;
                _timerText.DOColor(targetColor, 1f)
                    .SetId(_timerText);

                _timerText.transform.localScale = Vector3.one;
                _timerText.transform.DOScale(1.08f, 1f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetId(_timerText.transform);
            }

            if (seconds <= 60 && !_oneMinuteLeftShown)
            {
                _oneMinuteLeftPanel.SetActive(true);
                SoundManager.Instance.PlaySfxOneShot(SoundType.UI_WarningTime);
                _oneMinuteLeftShown = true;
            }
        }

        public void UpdateLivesUI(int lives)
        {
            for (int i = 0; i < LivesImages.Count; i++)
            {
                if (i < lives)
                {
                    LivesImages[i].sprite = FullLifeSprite;
                }
                else
                {
                    LivesImages[i].sprite = EmptyLifeSprite;
                }
            }
        }


    }
}