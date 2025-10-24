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
        private CircleCollider2D _collider;
        private float _currentSpawnDelay;
        private BallColor? _nextBallColor;
        private bool _isGeneratingBall = false;
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

        public void Init(LaunchContainer launchContainer)
        {
            _launchContainer = launchContainer;
            SpawnBall();
        }

        public void SpawnBall()
        {
            CurrentBall = SpawnRandomBall();
            CurrentBall.transform.localScale = Vector3.one;
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

            if (_nextBallColor.HasValue)
            {
                CurrentBall = SpawnBall(_nextBallColor.Value);
                _nextBallColor = null;
            }
            else
            {
                CurrentBall = SpawnRandomBall();
            }

            _isGeneratingBall = false;

            if (CurrentBall != null)
            {
                AnimateBallAppearance();
            }
        }

        public void SetNextBallColor(BallColor color)
        {
            _nextBallColor = color;
        }

        private void OnMouseDown()
        {
            if (!_enableSwapping) return;

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

            if (HapticManager.Exists())
            {
                HapticManager.Instance.TriggerHaptic(HapticType.Selection);
            }

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