using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Brain.Gameplay.Containers;
using TMPro;
using Brain.Gameplay;

namespace Brain.UI
{
    /// <summary>
    /// UI controller for bonus ball system
    /// </summary>
    public class BonusBallUI : MonoBehaviour
    {
        [Header("Power Meter")]
        [SerializeField] private Image _powerMeterFill;
        [SerializeField] private TextMeshProUGUI _powerPercentText;
        [SerializeField] private GameObject _powerMeterContainer;

        [Header("Bonus Button")]
        [SerializeField] private Button _bonusButton;
        [SerializeField] private Image _bonusButtonIcon;
        [SerializeField] private GameObject _bonusButtonGlow;
        [SerializeField] private GameObject _readyIndicator;

        [Header("Animation Settings")]
        [SerializeField] private float _fillAnimationDuration = 0.3f;
        [SerializeField] private float _glowPulseDuration = 1f;
        [SerializeField] private float _buttonShowDuration = 0.5f;

        [Header("References")]
        [SerializeField] private BonusBallContainer _bonusBallContainer;

        private BonusPowerManager _powerManager;
        private bool _isReady = false;
        private Sequence _glowSequence;

        private void Awake()
        {
            // Set initial states
            if (_bonusButton != null)
            {
                _bonusButton.gameObject.SetActive(false);
                _bonusButton.onClick.AddListener(OnBonusButtonClicked);
            }

            if (_bonusButtonGlow != null)
                _bonusButtonGlow.SetActive(false);

            if (_readyIndicator != null)
                _readyIndicator.SetActive(false);

            if (_powerMeterFill != null)
                _powerMeterFill.fillAmount = 0f;

            if (_powerPercentText != null)
                _powerPercentText.text = "0%";
        }

        private void Start()
        {
            _powerManager = BonusPowerManager.Instance;

            if (_bonusBallContainer == null)
            {
                _bonusBallContainer = FindObjectOfType<BonusBallContainer>();
            }
        }

        private void OnEnable()
        {
            BonusPowerManager.OnPowerChanged += OnPowerChanged;
            BonusPowerManager.OnBonusReady += OnBonusReady;
            BonusPowerManager.OnBonusActivated += OnBonusActivated;
            BonusPowerManager.OnBonusUsed += OnBonusUsed;
        }

        private void OnDisable()
        {
            BonusPowerManager.OnPowerChanged -= OnPowerChanged;
            BonusPowerManager.OnBonusReady -= OnBonusReady;
            BonusPowerManager.OnBonusActivated -= OnBonusActivated;
            BonusPowerManager.OnBonusUsed -= OnBonusUsed;

            _glowSequence?.Kill();
        }

        /// <summary>
        /// Called when power level changes
        /// </summary>
        private void OnPowerChanged(float normalizedPower)
        {
            // Update fill amount with animation
            if (_powerMeterFill != null)
            {
                _powerMeterFill.DOFillAmount(normalizedPower, _fillAnimationDuration);
            }

            // Update percentage text
            if (_powerPercentText != null)
            {
                int percentage = Mathf.RoundToInt(normalizedPower * 100f);
                _powerPercentText.text = $"{percentage}%";
            }
        }

        /// <summary>
        /// Called when bonus is ready
        /// </summary>
        private void OnBonusReady()
        {
            _isReady = true;

            // Show bonus button with animation
            if (_bonusButton != null)
            {
                _bonusButton.gameObject.SetActive(true);
                _bonusButton.transform.localScale = Vector3.zero;
                _bonusButton.transform.DOScale(1f, _buttonShowDuration).SetEase(Ease.OutBack);
            }

            // Show ready indicator
            if (_readyIndicator != null)
            {
                _readyIndicator.SetActive(true);
            }

            // Start glow pulse animation
            if (_bonusButtonGlow != null)
            {
                _bonusButtonGlow.SetActive(true);
                StartGlowAnimation();
            }

            // Add punch animation to power meter
            if (_powerMeterContainer != null)
            {
                _powerMeterContainer.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 4);
            }
        }

        /// <summary>
        /// Called when bonus is activated (button clicked)
        /// </summary>
        private void OnBonusActivated()
        {
            // Hide button
            if (_bonusButton != null)
            {
                _bonusButton.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => _bonusButton.gameObject.SetActive(false));
            }

            // Stop glow
            _glowSequence?.Kill();
            if (_bonusButtonGlow != null)
                _bonusButtonGlow.SetActive(false);
        }

        /// <summary>
        /// Called when bonus is used (shot)
        /// </summary>
        private void OnBonusUsed()
        {
            _isReady = false;

            // Hide ready indicator
            if (_readyIndicator != null)
            {
                _readyIndicator.SetActive(false);
            }

            // Reset power meter
            if (_powerMeterFill != null)
            {
                _powerMeterFill.DOFillAmount(0f, _fillAnimationDuration);
            }

            if (_powerPercentText != null)
            {
                _powerPercentText.text = "0%";
            }
        }

        /// <summary>
        /// Handle bonus button click
        /// </summary>
        private void OnBonusButtonClicked()
        {
            if (!_isReady || _powerManager == null || _bonusBallContainer == null)
                return;

            // Trigger ball swap
            //_bonusBallContainer.SwapWithLaunchContainer();

            // Button feedback
            if (_bonusButton != null)
            {
                _bonusButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            }
        }

        /// <summary>
        /// Start glow pulse animation
        /// </summary>
        private void StartGlowAnimation()
        {
            if (_bonusButtonGlow == null) return;

            _glowSequence?.Kill();
            _glowSequence = DOTween.Sequence();

            // Pulse scale
            _glowSequence.Append(_bonusButtonGlow.transform.DOScale(1.1f, _glowPulseDuration * 0.5f))
                .Append(_bonusButtonGlow.transform.DOScale(1f, _glowPulseDuration * 0.5f));

            // Fade alpha
            Image glowImage = _bonusButtonGlow.GetComponent<Image>();
            if (glowImage != null)
            {
                _glowSequence.Join(glowImage.DOFade(0.5f, _glowPulseDuration * 0.5f))
                    .Join(glowImage.DOFade(1f, _glowPulseDuration * 0.5f).SetDelay(_glowPulseDuration * 0.5f));
            }

            _glowSequence.SetLoops(-1);
        }

        private void OnDestroy()
        {
            _glowSequence?.Kill();

            if (_bonusButton != null)
            {
                _bonusButton.onClick.RemoveListener(OnBonusButtonClicked);
            }
        }
    }
}