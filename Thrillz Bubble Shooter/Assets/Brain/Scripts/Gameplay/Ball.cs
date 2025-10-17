using System;
using System.Collections.Generic;
using Brain.Util;
using UnityEngine;
using DG.Tweening;

namespace Brain.Gameplay
{
    /// <summary>
    /// Core ball component for bubble shooter
    /// Manages position, color, neighbors, and state flags
    /// Adapted from BubbleShooterGameToolkit reference implementation
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class Ball : MonoBehaviour
    {
        [Header("Ball Properties")]
        [SerializeField] private BallColor ballColor;

        [Header("Components")]
        private CircleCollider2D circleCollider;

        // Grid state
        public Vector2Int Position { get; private set; }
        public Ball[] Neighbors { get; private set; } = new Ball[6];

        // Ball state
        private BallFlags flags = BallFlags.None;
        public BallFlags Flags
        {
            get => flags;
            set
            {
                // Manage RootBalls HashSet when Root flag changes
                if (!HasFlag(BallFlags.Root) && (value & BallFlags.Root) != 0)
                {
                    RootBalls.Add(this);
                }
                else if (HasFlag(BallFlags.Root) && (value & BallFlags.Root) == 0)
                {
                    RootBalls.Remove(this);
                }

                flags = value;
            }
        }

        // Static collection of root balls (top row) for fast orphan detection
        public static readonly HashSet<Ball> RootBalls = new HashSet<Ball>();

        // Public accessors
        public BallColor Color => ballColor;

        // Events
        public Action<Ball> OnDestroyed;

        private void Awake()
        {
            circleCollider = GetComponent<CircleCollider2D>();
        }

        private void OnEnable()
        {
            // Reset state when ball is spawned/enabled
            Flags = BallFlags.None;
            Neighbors = new Ball[6];
        }

        private void OnDisable()
        {
            // Clear from RootBalls if present
            RootBalls.Remove(this);
        }

        /// <summary>
        /// Sets the ball's color (enum only, visual is handled by prefab)
        /// </summary>
        public void SetColor(BallColor color)
        {
            ballColor = color;
        }

        /// <summary>
        /// Sets the ball's grid position and world position
        /// </summary>
        public void SetPosition(Vector2Int gridPos, Vector3 worldPos)
        {
            Position = gridPos;
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

        /// <summary>
        /// Updates the neighbor references for this ball
        /// </summary>
        public void UpdateNeighbors(Ball[] neighbors)
        {
            Neighbors = neighbors;
        }

        /// <summary>
        /// Checks if ball has a specific flag
        /// </summary>
        public bool HasFlag(BallFlags flag)
        {
            return (Flags & flag) == flag;
        }

        /// <summary>
        /// Starts the ball falling (for orphaned balls)
        /// </summary>
        public void Fall()
        {
            // Mark as falling and unpin from grid
            Flags |= BallFlags.Falling;
            Flags &= ~BallFlags.Pinned;

            // Change to falling layer (to avoid collision with launched balls)
            gameObject.layer = LayerMask.NameToLayer("Default");

            // Add gravity-like downward movement
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.velocity = Vector2.down * 2f;

            // Add slight random rotation for visual effect
            transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-180f, 180f)), 1f);

            // Destroy after falling off screen
            Destroy(gameObject, 3f);
        }

        /// <summary>
        /// Destroys the ball with animation
        /// </summary>
        public void DestroyBall()
        {
            // Mark as destroying
            Flags |= BallFlags.Destroying;

            // Simple scale-down destruction animation
            transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                OnDestroyed?.Invoke(this);
                Destroy(gameObject);
            });
        }

        /// <summary>
        /// Enables/disables the collider
        /// </summary>
        public void SetColliderEnabled(bool enabled)
        {
            if (circleCollider != null)
            {
                circleCollider.enabled = enabled;
            }
        }

        /// <summary>
        /// Checks if this ball matches color with another ball
        /// </summary>
        public bool MatchesColor(Ball other)
        {
            if (other == null) return false;
            return ballColor == other.ballColor;
        }

        /// <summary>
        /// Returns true if ball has at least one valid neighbor
        /// </summary>
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
