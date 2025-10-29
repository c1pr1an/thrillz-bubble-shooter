using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Brain.Util;

namespace Brain.Gameplay
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class Ball : MonoBehaviour
    {
        // Static Fields
        public static readonly HashSet<Ball> s_rootBalls = new HashSet<Ball>();

        // Private Fields
        [Header("Ball Properties")]
        [SerializeField] private BallColor _ballColor;
        [SerializeField] private Color _displayColor;
        [SerializeField] private bool _isBonusBall;


        [Header("Components")]
        [SerializeField] private CircleCollider2D _circleCollider;
        [SerializeField] private Transform _model;
        [SerializeField] private GameObject _highlightSprite;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private BallFlags _flags = BallFlags.None;
        private RainbowBall _rainbowComponent;
        private BombBall _bombComponent;

        // Properties
        public Vector2Int GridPosition { get; private set; }
        public Ball[] Neighbors { get; private set; } = new Ball[6];
        public BallColor Color => _ballColor;
        public Color DisplayColor => _displayColor;
        public bool IsBonusBall => _isBonusBall;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public BallFlags Flags
        {
            get => _flags;
            set
            {
                // Manage s_rootBalls HashSet when Root flag changes
                if (!HasFlag(BallFlags.Root) && (value & BallFlags.Root) != 0)
                {
                    s_rootBalls.Add(this);
                }
                else if (HasFlag(BallFlags.Root) && (value & BallFlags.Root) == 0)
                {
                    s_rootBalls.Remove(this);
                }

                _flags = value;
            }
        }

        // Events
        public Action<Ball> OnDestroyed;

        private void Awake()
        {
            if (_isBonusBall)
            {
                _rainbowComponent = GetComponent<RainbowBall>();
                _bombComponent = GetComponent<BombBall>();
            }
        }

        private void OnEnable()
        {
            // Reset state when ball is spawned/enabled
            transform.localScale = Vector3.one;
            if (_model != null)
            {
                _model.localScale = Vector3.one;
            }
            Flags = BallFlags.None;
            Neighbors = new Ball[6];
        }

        private void OnDisable()
        {
            // Clear from s_rootBalls if present
            s_rootBalls.Remove(this);
        }

        public void SetColor(BallColor color)
        {
            _ballColor = color;
        }

        public void SetPosition(Vector2Int gridPos, Vector3 worldPos)
        {
            GridPosition = gridPos;
            transform.position = worldPos;

            // Mark as pinned (static on grid)
            Flags |= BallFlags.Pinned;

            // Top rows are ceiling (balls hang from top)
            if (gridPos.y >= 60)
            {
                Flags |= BallFlags.Root;
            }
            else
            {
                Flags &= ~BallFlags.Root;
            }

            // Enable collider for grid balls
            SetColliderEnabled(true);
        }

        public void UpdateNeighbors(Ball[] neighbors)
        {
            Neighbors = neighbors;
        }

        public bool HasFlag(BallFlags flag)
        {
            return (Flags & flag) == flag;
        }

        public void Fall()
        {
            Flags |= BallFlags.Falling;
            Flags &= ~BallFlags.Pinned;

            OnDestroyed?.Invoke(this);

            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.velocity = Vector2.down * 4f;
            rb.angularVelocity = UnityEngine.Random.Range(-150f, 150f);
            _circleCollider.enabled = false;

            Invoke("ReturnToPool", 2f);
        }

        public void DestroyBall()
        {
            // Mark as destroying
            Flags |= BallFlags.Destroying;

            // Simple scale-down destruction animation
            transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                OnDestroyed?.Invoke(this);
                ReturnToPool();
            });
        }

        public void ReturnToPool()
        {
            transform.DOKill();
            _model.DOKill();

            Flags = BallFlags.None;
            Neighbors = new Ball[6];

            if (GetComponent<Rigidbody2D>() != null)
                Destroy(GetComponent<Rigidbody2D>());

            gameObject.layer = 0;
            transform.localScale = Vector3.one;
            if (_model != null) _model.localScale = Vector3.one;

            OnDestroyed?.Invoke(this);
            ObjectPooler.Instance.Release(gameObject, _ballColor);
        }

        public void SetColliderEnabled(bool enabled)
        {
            if (_circleCollider != null)
            {
                _circleCollider.enabled = enabled;
            }
        }

        public void AnimateScaleTo(float targetScale, float duration = 0.3f, TweenCallback onComplete = null)
        {
            _model.DOKill(complete: false);
            _model.DOScale(Vector3.one * targetScale, duration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void AnimateScaleUp(float duration = 0.3f, TweenCallback onComplete = null)
        {
            AnimateScaleTo(1.2f, duration, onComplete);
        }

        public void AnimateScaleDown(float duration = 0.3f, TweenCallback onComplete = null)
        {
            AnimateScaleTo(1f, duration, onComplete);
        }

        public bool MatchesColor(Ball other)
        {
            if (other == null) return false;

            // Rainbow balls match with any color
            if (IsRainbow() || other.IsRainbow())
                return true;

            return _ballColor == other._ballColor;
        }

        /// <summary>
        /// Check if this ball is a Rainbow bonus ball
        /// </summary>
        public bool IsRainbow()
        {
            return _rainbowComponent != null && _rainbowComponent.enabled && _rainbowComponent.CanMatchAnyColor();
        }

        /// <summary>
        /// Check if this ball is a Bomb bonus ball
        /// </summary>
        public bool IsBomb()
        {
            return _bombComponent != null && _bombComponent.enabled && _bombComponent.IsBomb();
        }

        /// <summary>
        /// Set the highlight state of this ball
        /// </summary>
        public void SetHighlight(bool enabled)
        {
            if (_highlightSprite != null)
            {
                _highlightSprite.SetActive(enabled);
            }
        }

        public bool HasValidNeighbor()
        {
            foreach (var neighbor in Neighbors)
            {
                if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                {
                    return true;
                }
            }
            return false;
        }

        public void PlayWaveAnimation(Vector3 direction, float amplitude, float duration, Ease easeType)
        {
            // Prevent overlapping wave animations
            if (HasFlag(BallFlags.AnimatingWave)) return;

            // Set animating flag
            Flags |= BallFlags.AnimatingWave;

            // Create bounce sequence
            Sequence waveSequence = DOTween.Sequence();

            // Phase 1: Move outward in wave direction
            waveSequence.Append(_model.DOLocalMove(direction * amplitude, duration)
                .SetEase(easeType));

            // Phase 2: Return to original position
            waveSequence.Append(_model.DOLocalMove(Vector3.zero, duration)
                .SetEase(Ease.InOutSine));

            // Clear flag when animation completes
            waveSequence.OnComplete(() =>
            {
                Flags &= ~BallFlags.AnimatingWave;
            });
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Draw lines to all neighbors
            Gizmos.color = UnityEngine.Color.cyan;
            for (int i = 0; i < Neighbors.Length; i++)
            {
                if (Neighbors[i] != null)
                {
                    Gizmos.DrawLine(transform.position, Neighbors[i].transform.position);
                }
            }

            // Draw root balls in green
            if (HasFlag(BallFlags.Root))
            {
                Gizmos.color = UnityEngine.Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.6f);
            }
        }
#endif
    }
}
