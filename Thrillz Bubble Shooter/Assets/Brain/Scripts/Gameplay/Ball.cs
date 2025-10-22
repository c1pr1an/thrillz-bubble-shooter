using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

        [Header("Components")]
        private CircleCollider2D _circleCollider;
        private BallFlags _flags = BallFlags.None;

        // Properties
        public Vector2Int Position { get; private set; }
        public Ball[] Neighbors { get; private set; } = new Ball[6];
        public BallColor Color => _ballColor;
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
            _circleCollider = GetComponent<CircleCollider2D>();
        }

        private void OnEnable()
        {
            // Reset state when ball is spawned/enabled
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
            rb.velocity = Vector2.down * 2f;
            _circleCollider.enabled = false;

            transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-180f, 180f)), 1f);

            Destroy(gameObject, 3f);
        }

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

        public void SetColliderEnabled(bool enabled)
        {
            if (_circleCollider != null)
            {
                _circleCollider.enabled = enabled;
            }
        }

        public bool MatchesColor(Ball other)
        {
            if (other == null) return false;
            return _ballColor == other._ballColor;
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
