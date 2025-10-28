using System.Collections;
using UnityEngine;
using DG.Tweening;
using Brain.Managers;

namespace Brain.Gameplay.Containers
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class BonusBallContainer : BallContainerBase
    {
        [Header("Bonus Ball Settings")]
        [SerializeField] private GameObject _rainbowBallPrefab;

        [Header("Testing Mode")]
        [SerializeField] private bool _testMode = true; // Enable for testing without UI
        [SerializeField] private bool _spawnOnStart = true; // Spawn bonus ball immediately

        private LaunchContainer _launchContainer;
        private CircleCollider2D _collider;
        private bool _enableSwapping = true;

        protected override void Awake()
        {
            base.Awake();

            _collider = GetComponent<CircleCollider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
                _collider.radius = 0.5f;
            }
        }

        /// <summary>
        /// Initialize the bonus ball container
        /// </summary>
        public void Init()
        {
            // Get reference to launch container
            if (GridManager.Instance != null)
            {
                _launchContainer = GridManager.Instance.BallLaunchContainer;
            }

            // In test mode, spawn ball immediately
            if (_testMode && _spawnOnStart)
            {
                SpawnBonusBall();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Only subscribe to events if not in test mode
            if (!_testMode)
            {
                BonusPowerManager.OnBonusReady += OnBonusReady;
                BonusPowerManager.OnBonusUsed += OnBonusUsed;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!_testMode)
            {
                BonusPowerManager.OnBonusReady -= OnBonusReady;
                BonusPowerManager.OnBonusUsed -= OnBonusUsed;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T) && _testMode)
            {
                if (_testMode && _enableSwapping && CanSwapWithLauncher())
                {
                    SwapWithLauncher();
                }
            }
        }

        private bool CanSwapWithLauncher()
        {
            return _launchContainer != null &&
                   _launchContainer.CanLaunch &&
                   _launchContainer.HasBall &&
                   HasBall &&
                   !IsSwapping &&
                   !_launchContainer.IsSwapping;
        }

        /// <summary>
        /// Swap balls with launch container using base class method
        /// </summary>
        public void SwapWithLauncher()
        {
            if (!CanSwapWithLauncher())
                return;

            // Visual feedback
            AnimateSwapFeedback();

            if (HapticManager.Exists())
            {
                HapticManager.Instance.TriggerHaptic(HapticType.Selection);
            }

            // Use base class swap method (same as preview container)
            SwapBalls(_launchContainer);

            // Notify that bonus is now active (only if not in test mode)
            if (!_testMode && BonusPowerManager.Exists())
            {
                BonusPowerManager.Instance.ActivateBonus();
            }


            Debug.Log("[BonusBallContainer] Swapped with launch container!");
        }

        private void AnimateSwapFeedback()
        {

        }

        protected override void OnBallReleased(Ball ball)
        {
            DOVirtual.DelayedCall(0.1f, () => { SpawnBonusBall(); });
        }

        /// <summary>
        /// Called when bonus power is ready
        /// </summary>
        private void OnBonusReady()
        {
            SpawnBonusBall();
        }

        /// <summary>
        /// Called when bonus ball is used
        /// </summary>
        private void OnBonusUsed()
        {
            // Bonus ball is automatically respawned after swap, no need to do anything here
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

            var bonusType = BonusBallType.Rainbow;//BonusPowerManager.Instance.GetRandomBonusType();

            GameObject prefab = null;
            switch (bonusType)
            {
                case BonusBallType.Rainbow:
                    prefab = _rainbowBallPrefab;
                    break;
                // Add other bonus types here in future
                default:
                    Debug.LogError($"[BonusBallContainer] No prefab for bonus type {bonusType}");
                    return;
            }

            if (prefab == null)
            {
                Debug.LogError("[BonusBallContainer] Rainbow ball prefab not assigned!");
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

            // Set as bonus ball
            var rainbowComponent = CurrentBall.GetComponent<RainbowBall>();
            if (rainbowComponent != null)
            {
                rainbowComponent.enabled = true;
            }

            // Visual spawn effect
            CurrentBall.transform.localScale = Vector3.zero;
            CurrentBall.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            Debug.Log($"[BonusBallContainer] Spawned {bonusType} bonus ball");
        }


        public void SetSwappingEnabled(bool enabled)
        {
            _enableSwapping = enabled;

            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }
    }
}