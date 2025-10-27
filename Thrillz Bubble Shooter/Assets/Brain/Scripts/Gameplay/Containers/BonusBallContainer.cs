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

        [Header("References")]
        [SerializeField] private Transform _circleArrows; // For visual feedback

        private Ball _storedNormalBall = null;
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
                SpawnBonusBallForTesting();
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

            // Store the normal ball that will come to us
            _storedNormalBall = _launchContainer.CurrentBall;

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
            if (_circleArrows != null)
            {
                _circleArrows.DOPunchScale(Vector3.one * 0.05f, 0.4f, 3, 0.5f);
                _circleArrows.DORotate(new Vector3(0, 0, 180), 0.4f, RotateMode.LocalAxisAdd);
            }
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
            // In test mode, respawn bonus ball after a delay
            if (_testMode)
            {
                StartCoroutine(RespawnBonusBallAfterDelay());
            }
        }

        /// <summary>
        /// Spawn a bonus ball for testing (bypasses power system)
        /// </summary>
        private void SpawnBonusBallForTesting()
        {
            if (CurrentBall != null)
            {
                Debug.LogWarning("[BonusBallContainer] Already has a ball!");
                return;
            }

            if (_rainbowBallPrefab == null)
            {
                Debug.LogError("[BonusBallContainer] Rainbow ball prefab not assigned!");
                return;
            }

            var ballGO = Instantiate(_rainbowBallPrefab, _ballHolder.position, Quaternion.identity, _ballHolder);
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

            Debug.Log("[BonusBallContainer] TEST MODE: Spawned Rainbow ball for testing");
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

        /// <summary>
        /// Called when a ball is received in this container
        /// </summary>
        protected override void OnBallReceived(Ball ball)
        {
            base.OnBallReceived(ball);

            // If we received a normal ball and it's the stored one, it means the bonus ball was shot
            if (ball == _storedNormalBall && !ball.IsRainbow())
            {
                // The normal ball is now in our container after swap
                // We'll wait for it to be shot, then respawn bonus ball
                StartCoroutine(WaitForBonusBallUsage());
            }
        }

        private IEnumerator WaitForBonusBallUsage()
        {
            // Wait for the launch container to shoot the bonus ball
            yield return new WaitForSeconds(0.5f);

            // Check if launch container no longer has a ball (meaning it was shot)
            while (_launchContainer != null && _launchContainer.HasBall)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Bonus ball was shot, notify system
            if (!_testMode && BonusPowerManager.Exists())
            {
                BonusPowerManager.Instance.UsedBonus();
            }

            // In test mode, respawn after delay
            if (_testMode)
            {
                yield return new WaitForSeconds(2f);

                // Clear the stored normal ball
                if (CurrentBall == _storedNormalBall)
                {
                    CurrentBall = null;
                    _storedNormalBall = null;
                }

                SpawnBonusBallForTesting();
            }
        }

        private IEnumerator RespawnBonusBallAfterDelay()
        {
            yield return new WaitForSeconds(2f);

            if (CurrentBall == null)
            {
                SpawnBonusBallForTesting();
            }
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