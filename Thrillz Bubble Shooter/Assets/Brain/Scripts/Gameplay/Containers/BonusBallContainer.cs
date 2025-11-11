using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Brain.Managers;
using Brain.Gameplay;
using Brain.Audio;

namespace Brain.Gameplay.Containers
{
    public enum BonusBallState
    {
        Charging,
        ReadyToUse,
        ReadyToAutoSwap,
        Active
    }

    [RequireComponent(typeof(CircleCollider2D))]
    public class BonusBallContainer : BallContainerBase
    {
        [Header("Bonus Ball Settings")]
        [SerializeField] private GameObject _rainbowBallPrefab;
        [SerializeField] private GameObject _bombBallPrefab;
        [SerializeField] private GameObject _lightningBallPrefab;
        [SerializeField] private GameObject _rocketBallPrefab;

        [Header("Charge UI")]
        [SerializeField] private Image _chargeProgressFill;
        [SerializeField] private ParticleSystem _readyParticles;
        [SerializeField] private ParticleSystem _readyIdleParticles;

        private LaunchContainer _launchContainer;
        private CircleCollider2D _collider;
        private bool _enableSwapping = true;
        private BonusBallState _currentState;
        private bool _shouldTransitionToAvailable = false;

        public BonusBallState CurrentState => _currentState;

        protected override void Awake()
        {
            base.Awake();
            _collider = GetComponent<CircleCollider2D>();
        }

        public void Init(LaunchContainer launchContainer)
        {
            _launchContainer = launchContainer;
            _launchContainer.OnBallLaunched += OnBallLaunched;

            SpawnBonusBall();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            BonusPowerManager.OnPowerChanged += OnPowerChanged;
            BonusPowerManager.OnBonusReady += OnBonusReady;
            BonusPowerManager.OnBonusUsed += OnBonusUsed;

            // Subscribe to InputManager click event
            InputManager.OnBonusContainerClicked += OnContainerClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            BonusPowerManager.OnPowerChanged -= OnPowerChanged;
            BonusPowerManager.OnBonusReady -= OnBonusReady;
            BonusPowerManager.OnBonusUsed -= OnBonusUsed;
            _launchContainer.OnBallLaunched -= OnBallLaunched;

            // Unsubscribe from InputManager click event
            InputManager.OnBonusContainerClicked -= OnContainerClicked;
        }

        private void OnContainerClicked(BonusBallContainer container)
        {
            // Only process if this is the clicked container
            if (container != this) return;

            if (_enableSwapping && CanSwapWithLauncher())
            {
                SwapWithLauncher();
            }
        }

        private bool CanSwapWithLauncher()
        {
            return (_currentState == BonusBallState.ReadyToUse || _currentState == BonusBallState.ReadyToAutoSwap) &&
                   _launchContainer != null &&
                   _launchContainer.CanLaunch &&
                   _launchContainer.HasBall &&
                   HasBall &&
                   !IsSwapping &&
                   !_launchContainer.IsSwapping;
        }

        public void SwapWithLauncher()
        {
            if (!CanSwapWithLauncher())
                return;

            AnimateSwapFeedback();
            HapticManager.Instance.TriggerHaptic(HapticType.Selection);

            SwapBalls(_launchContainer);
            BonusPowerManager.Instance.UsedBonus();
            Debug.Log("[BonusBallContainer] Swapped with launch container!");
        }

        private void AnimateSwapFeedback()
        {

        }

        protected override void OnBallReleased(Ball ball)
        {
            DOVirtual.DelayedCall(0.1f, () => { SpawnBonusBall(); });
        }

        private void OnPowerChanged(float normalizedPower)
        {
            _chargeProgressFill.DOKill();
            if (normalizedPower == 0)
                _chargeProgressFill.fillAmount = 0f;
            else
                _chargeProgressFill.DOFillAmount(normalizedPower, 0.3f).SetEase(Ease.OutQuad);
        }

        private void OnBonusReady()
        {
            _readyParticles.gameObject.SetActive(true);
            _readyParticles.Play();
            SoundManager.Instance.PlaySfxOneShot(SoundType.Game_BonusBallReady);
            _readyIdleParticles.gameObject.SetActive(true);
            SetBallVisualState(BonusBallState.ReadyToUse);
            _shouldTransitionToAvailable = true;
        }

        private void OnBonusUsed()
        {
            _readyIdleParticles.gameObject.SetActive(false);
            SetBallVisualState(BonusBallState.Charging);
            _shouldTransitionToAvailable = false;
        }

        public void OnBallLaunched(Ball ball)
        {
            if (_shouldTransitionToAvailable && _currentState == BonusBallState.ReadyToUse)
            {
                SetBallVisualState(BonusBallState.ReadyToAutoSwap);
                _shouldTransitionToAvailable = false;
            }
        }

        /// <summary>
        /// Spawn a bonus ball based on type
        /// </summary>
        private void SpawnBonusBall()
        {
            if (CurrentBall != null)
            {
                Debug.LogWarning("[BonusBallContainer] Already has a ball!");
                return;
            }

            var bonusType = BonusPowerManager.Instance.GetRandomBonusType();

            GameObject prefab = null;
            switch (bonusType)
            {
                case BonusBallType.Rainbow:
                    prefab = _rainbowBallPrefab;
                    break;
                case BonusBallType.Bomb:
                    prefab = _bombBallPrefab;
                    break;
                case BonusBallType.Lightning:
                    prefab = _lightningBallPrefab;
                    break;
                case BonusBallType.Rocket:
                    prefab = _rocketBallPrefab;
                    break;
                // Add other bonus types here in future
                default:
                    Debug.LogError($"[BonusBallContainer] No prefab for bonus type {bonusType}");
                    return;
            }

            if (prefab == null)
            {
                Debug.LogError($"[BonusBallContainer] {bonusType} ball prefab not assigned!");
                return;
            }

            var ballGO = Instantiate(prefab, _ballHolder.position, Quaternion.identity, _ballHolder);
            CurrentBall = ballGO.GetComponent<Ball>();

            if (CurrentBall == null)
            {
                Debug.LogError("[BonusBallContainer] Spawned prefab doesn't have Ball component!");
                Destroy(ballGO);
                return;
            }

            // Disable ball's collider so it doesn't block container clicks
            CurrentBall.SetColliderEnabled(false);

            // Visual spawn effect
            CurrentBall.transform.localScale = Vector3.zero;
            CurrentBall.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // Set initial charging state
            SetBallVisualState(BonusBallState.Charging);

            Debug.Log($"[BonusBallContainer] Spawned {bonusType} bonus ball");
        }

        private void SetBallVisualState(BonusBallState state)
        {
            _currentState = state;

            switch (state)
            {
                case BonusBallState.Charging:
                    SetBallAlpha(0.5f);
                    SetColliderEnabled(false);
                    CurrentBall?.SetHighlight(false);
                    break;

                case BonusBallState.ReadyToUse:
                case BonusBallState.ReadyToAutoSwap:
                    SetBallAlpha(1.0f);
                    SetColliderEnabled(true);
                    CurrentBall.SetHighlight(true);

                    // Activate idle particles for bonus balls
                    if (CurrentBall != null)
                    {
                        BonusBallBase bonusBall = CurrentBall.GetComponent<BonusBallBase>();
                        if (bonusBall != null)
                        {
                            bonusBall.ActivateIdleParticles();
                        }
                    }
                    break;

                case BonusBallState.Active:
                    SetBallAlpha(1.0f);
                    CurrentBall.SetHighlight(true);

                    // Activate idle particles for bonus balls
                    if (CurrentBall != null)
                    {
                        BonusBallBase activeBonusBall = CurrentBall.GetComponent<BonusBallBase>();
                        if (activeBonusBall != null)
                        {
                            activeBonusBall.ActivateIdleParticles();
                        }
                    }
                    break;
            }
        }

        private void SetBallAlpha(float alpha)
        {
            if (CurrentBall != null)
            {
                Color color = CurrentBall.SpriteRenderer.color;
                color.a = alpha;
                CurrentBall.SpriteRenderer.color = color;
            }
        }

        private void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
            {
                _collider.enabled = enabled && _enableSwapping;
            }
        }

        public void SetSwappingEnabled(bool enabled)
        {
            _enableSwapping = enabled;
            bool canSwap = (_currentState == BonusBallState.ReadyToUse || _currentState == BonusBallState.ReadyToAutoSwap) && enabled;
            SetColliderEnabled(canSwap);
        }
    }
}