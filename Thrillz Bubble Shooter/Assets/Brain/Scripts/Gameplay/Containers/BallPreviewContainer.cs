using UnityEngine;
using DG.Tweening;
using Brain.Managers;

namespace Brain.Gameplay.Containers
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallPreviewContainer : BallContainerBase
    {
        [Header("Preview Settings")]
        [SerializeField] private float _spawnDelay = 0.5f;

        [Header("References")]
        [SerializeField] private Transform _circleArrows;

        private LaunchContainer _launchContainer;
        private BonusBallContainer _bonusBallContainer;
        private float _currentSpawnDelay;
        private BallColor? _nextBallColor;
        private bool _isGeneratingBall = false;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Subscribe to InputManager click event
            InputManager.OnPreviewContainerClicked += OnContainerClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Unsubscribe from InputManager click event
            InputManager.OnPreviewContainerClicked -= OnContainerClicked;
        }

        public void Init(LaunchContainer launchContainer, BonusBallContainer bonusContainer)
        {
            _launchContainer = launchContainer;
            _bonusBallContainer = bonusContainer;

            SpawnBall();
        }

        public void SpawnBall()
        {
            CurrentBall = SpawnRandomBall();
            CurrentBall.transform.localScale = Vector3.one;

            // Disable ball's collider so it doesn't block container clicks
            if (CurrentBall != null)
            {
                CurrentBall.SetColliderEnabled(false);
            }
        }

        private void Update()
        {
            if (CurrentBall == null && !_isGeneratingBall && !IsSwapping)
            {
                if (_currentSpawnDelay > 0)
                {
                    _currentSpawnDelay -= Time.deltaTime;
                }
                else
                {
                    GenerateNextBall();
                }
            }
        }

        private void GenerateNextBall()
        {
            if (_isGeneratingBall) return;

            _isGeneratingBall = true;
            _currentSpawnDelay = _spawnDelay;

            if (_bonusBallContainer.CurrentBall != null && _bonusBallContainer.CurrentState == BonusBallState.ReadyToAutoSwap)
            {
                _bonusBallContainer.SwapBalls(this);
                BonusPowerManager.Instance.UsedBonus();
            }
            else if (_nextBallColor.HasValue)
            {
                CurrentBall = SpawnBall(_nextBallColor.Value);
                _nextBallColor = null;

                if (CurrentBall != null)
                {
                    CurrentBall.SetColliderEnabled(false);
                    AnimateBallAppearance();
                }
            }
            else
            {
                CurrentBall = SpawnRandomBall();

                if (CurrentBall != null)
                {
                    CurrentBall.SetColliderEnabled(false);
                    AnimateBallAppearance();
                }
            }

            _isGeneratingBall = false;
        }

        public void SetNextBallColor(BallColor color)
        {
            _nextBallColor = color;
        }

        private void OnContainerClicked(BallPreviewContainer container)
        {
            // Only process if this is the clicked container
            if (container != this) return;

            if (CanSwapWithLauncher())
            {
                SwapWithLauncher();
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

        public void SwapWithLauncher()
        {
            AnimateSwapFeedback();
            HapticManager.Instance.TriggerHaptic(HapticType.Selection);
            SwapBalls(_launchContainer);
            _currentSpawnDelay = _spawnDelay;
        }

        protected override void OnBallReleased(Ball ball)
        {
            base.OnBallReleased(ball);
            _currentSpawnDelay = _spawnDelay;
        }

        private void AnimateBallAppearance()
        {
            if (CurrentBall == null) return;

            CurrentBall.transform.localScale = Vector3.zero;
            CurrentBall.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack);
        }

        private void AnimateSwapFeedback()
        {
            _circleArrows.DOPunchScale(Vector3.one * 0.05f, 0.4f, 3, 0.5f);
            _circleArrows.DORotate(new Vector3(0, 0, 180), 0.4f, RotateMode.LocalAxisAdd);
        }
    }
}