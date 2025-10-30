using System.Collections.Generic;
using UnityEngine;
using Brain.Managers;
using Brain.Util;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Rainbow bonus ball.
    /// Rainbow balls match with ANY color they touch.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class RainbowBall : MonoBehaviour, IBonusBallDetector
    {
        private Ball _ball;

        [Header("Visual Settings")]
        [SerializeField] private bool _enableRainbowEffect = true;
        [SerializeField] private float _colorCycleSpeed = 2f;

        private SpriteRenderer _spriteRenderer;
        private float _hue = 0f;

        private void Awake()
        {
            _ball = GetComponent<Ball>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // Optional rainbow color cycling effect
            if (_enableRainbowEffect && _spriteRenderer != null)
            {
                _hue += _colorCycleSpeed * Time.deltaTime;
                if (_hue > 1f) _hue -= 1f;

                Color rainbowColor = Color.HSVToRGB(_hue, 0.7f, 1f);
                _spriteRenderer.color = rainbowColor;
            }
        }

        /// <summary>
        /// Check if this ball should match with another ball regardless of color
        /// </summary>
        public bool CanMatchAnyColor()
        {
            return enabled && _ball != null;
        }

        #region IBonusBallDetector Implementation

        /// <summary>
        /// Get all balls that would be matched by the rainbow ball (color groups of 3+)
        /// </summary>
        public List<Ball> GetAffectedBalls(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            List<Ball> affectedBalls = new List<Ball>();
            HashSet<Ball> processedBalls = new HashSet<Ball>();

            if (_ball == null)
                return affectedBalls;

            // Get grid manager
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
                return affectedBalls;

            // Determine which neighbors to use
            List<Ball> neighbors = new List<Ball>();

            // If impact position is provided and different from ball position, get neighbors at that position (for preview)
            if (impactPosition != Vector2.zero && (Vector2)_ball.transform.position != impactPosition)
            {
                // Convert impact position to grid position
                Vector2Int gridPos = GridUtils.WorldToPos(impactPosition, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);

                // Get neighbor positions
                Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(gridPos);
                foreach (Vector2Int? pos in neighborPositions)
                {
                    if (pos.HasValue)
                    {
                        Ball neighbor = gridManager.GetBall(pos.Value.x, pos.Value.y);
                        if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                        {
                            neighbors.Add(neighbor);
                        }
                    }
                }
            }
            else
            {
                // Use ball's actual neighbors (for actual detection after landing)
                neighbors.AddRange(_ball.Neighbors);
            }

            // Track which colors to check
            HashSet<BallColor> colorsToCheck = new HashSet<BallColor>();

            // First, identify all colors directly touching the rainbow ball
            foreach (Ball neighbor in neighbors)
            {
                if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                {
                    colorsToCheck.Add(neighbor.Color);
                }
            }

            // For each color, check if we have 3+ balls (including the rainbow)
            foreach (BallColor color in colorsToCheck)
            {
                List<Ball> colorGroup = new List<Ball>();

                // Find all neighbors of this color and flood fill from one
                Ball startBall = null;
                foreach (Ball neighbor in neighbors)
                {
                    if (neighbor != null && neighbor.Color == color &&
                        neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                    {
                        startBall = neighbor;
                        break;
                    }
                }

                if (startBall == null) continue;

                // Find all connected balls of this color using flood fill
                Queue<Ball> toCheck = new Queue<Ball>();
                HashSet<Ball> visited = new HashSet<Ball>();
                toCheck.Enqueue(startBall);

                while (toCheck.Count > 0)
                {
                    Ball current = toCheck.Dequeue();
                    if (visited.Contains(current)) continue;

                    visited.Add(current);
                    colorGroup.Add(current);

                    // Check neighbors of same color
                    foreach (Ball n in current.Neighbors)
                    {
                        if (n != null && n.Color == color &&
                            n.HasFlag(BallFlags.Pinned) && !n.HasFlag(BallFlags.Destroying) &&
                            !visited.Contains(n))
                        {
                            toCheck.Enqueue(n);
                        }
                    }
                }

                // If this color group + rainbow ball makes 3 or more, include them
                if (colorGroup.Count >= 2) // 2 color balls + 1 rainbow = 3 total
                {
                    foreach (Ball ball in colorGroup)
                    {
                        if (!processedBalls.Contains(ball))
                        {
                            affectedBalls.Add(ball);
                            processedBalls.Add(ball);
                            ball.Flags |= BallFlags.MarkedForMatch;
                        }
                    }
                }
            }

            // Only add rainbow ball if we found valid matches
            if (affectedBalls.Count > 0)
            {
                affectedBalls.Add(_ball);
                _ball.Flags |= BallFlags.MarkedForMatch;
            }

            return affectedBalls;
        }

        /// <summary>
        /// Draw debug visualization for rainbow ball matches
        /// </summary>
        public void DrawDebugVisualization(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            if (_ball == null)
                return;

            // Draw a rainbow-colored circle around the impact position
            float radius = 0.5f;
            int segments = 32;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 point1 = impactPosition + new Vector2(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
                Vector3 point2 = impactPosition + new Vector2(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);

                // Create rainbow colors
                float hue = (float)i / segments;
                Gizmos.color = Color.HSVToRGB(hue, 1f, 1f);
                Gizmos.DrawLine(point1, point2);
            }

            // Draw center point
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(impactPosition, 0.15f);
        }

        #endregion

        private void OnEnable()
        {
            Debug.Log("[RainbowBall] Rainbow ball activated!");
        }

        private void OnDisable()
        {
            // Reset color when disabled
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
        }
    }
}