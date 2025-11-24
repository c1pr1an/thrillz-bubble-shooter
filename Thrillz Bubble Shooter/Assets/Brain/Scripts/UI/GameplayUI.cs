using System.Collections;
using System.Collections.Generic;
using Brain.Audio;
using Brain.Gameplay;
using Brain.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Brain.UI
{
    public class GameplayUI : MonoBehaviour
    {
        // Serialized Fields
        [SerializeField] private GameObject _oneMinuteLeftPanel;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private RectTransform _topUiBoundaryReference;

        // Private Fields
        private bool _oneMinuteLeftShown = false;
        private bool _isTimeTextPulsing = false;

        // Properties
        public TextMeshProUGUI ScoreText
        {
            get { return _scoreText; }
        }

        public float GetTopBoundaryY()
        {
            //if (_topBoundaryY != 0f) return _topBoundaryY;
            float topBoundaryY = 0f;

            // Get the world corners of the UI element
            Vector3[] corners = new Vector3[4];
            _topUiBoundaryReference.GetWorldCorners(corners);

            if (UIManager.Instance.Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Vector3 screenPos = corners[0];
                Vector3 worldPos = Cameras.Instance.MainCam.ScreenToWorldPoint(screenPos);
                topBoundaryY = worldPos.y;
            }
            else
            {
                topBoundaryY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            }

            return topBoundaryY;
        }

        public float GetCeilingY()
        {
            float uiBoundary = GetTopBoundaryY();
            float gridTop = GridManager.Instance.GetTopMostRowWorldY();
            if (gridTop == float.MaxValue) return uiBoundary;
            float effectiveGridTop = gridTop + GridManager.Instance.BallHeight * 0.5f;
            return Mathf.Min(uiBoundary, effectiveGridTop);
        }

        public void Start()
        {
            GameConditionsManager.Instance.OnTimerUpdated += SetGameTime;
        }

        public void OnDisable()
        {
            if (GameConditionsManager.Instance != null)
            {
                GameConditionsManager.Instance.OnTimerUpdated -= SetGameTime;
            }
        }

        // Public Methods
        public void PauseOnClick()
        {
            GameController.Instance.RestartGame();
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
                StartCoroutine(HideOneMinuteLeftPanel());
            }
        }

        private IEnumerator HideOneMinuteLeftPanel()
        {
            yield return new WaitForSeconds(3f);
            _oneMinuteLeftPanel.SetActive(false);
        }

    }
}