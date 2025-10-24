using UnityEngine;
using DG.Tweening;
using Brain.Managers;

namespace Brain.Gameplay.Containers
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallPreviewContainer : BallContainerBase
    {
        [Header("Preview Settings")]
        [SerializeField] private LaunchContainer _launchContainer;
        [SerializeField] private float _spawnDelay = 0.5f;
        [SerializeField] private bool _enableSwapping = true;

        [Header("Visual Feedback")]
        [SerializeField] private float _swapScalePunch = 0.2f;
        [SerializeField] private float _swapScaleDuration = 0.3f;

        private CircleCollider2D _collider;
        private float _currentSpawnDelay;
        private BallColor? _nextBallColor;
        private bool _isGeneratingBall = false;

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

        private void Start()
        {
            _currentSpawnDelay = 0f;
            GenerateNextBall();
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
            if (!CanSwapWithLauncher()) return;

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
            transform.DOPunchScale(Vector3.one * _swapScalePunch, _swapScaleDuration, 3, 0.5f);
        }

        public void SetSwappingEnabled(bool enabled)
        {
            _enableSwapping = enabled;

            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (_collider != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, _collider.radius);
            }

            if (_launchContainer != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _launchContainer.transform.position);
            }
        }
#endif
    }
}