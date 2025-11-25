using System.Collections.Generic;
using UnityEngine;
using Brain.Managers;
using Brain.Util;
using Brain.Audio;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Rainbow bonus ball.
    /// Rainbow balls match with ANY color they touch.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class RainbowBall : BonusBallBase
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

        public override void ActivateIdleParticles()
        {
            base.ActivateIdleParticles();
            SoundManager.Instance.PlaySfxLoop(SoundType.Game_Magic_Loop);
        }

        /// <summary>
        /// Check if this ball should match with another ball regardless of color
        /// </summary>
        public bool CanMatchAnyColor()
        {
            return enabled && _ball != null;
        }

        #region BonusBall Implementation

        /// <summary>
        /// Get all balls that would be matched by the rainbow ball (color groups of 3+)
        /// </summary>
        public override List<Ball> GetAffectedBalls(Vector2 impactPosition, Vector2 impactDirection = default)
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
                foreach (Ball n in _ball.Neighbors)
                {
                    if (n != null && n != _ball) // Make sure we don't add the rainbow ball itself
                    {
                        neighbors.Add(n);
                    }
                }
            }

            // Track which colors to check
            HashSet<BallColor> colorsToCheck = new HashSet<BallColor>();
            List<BallColor> availableColors = new();

            // First, identify all colors directly touching the rainbow ball
            foreach (Ball neighbor in neighbors)
            {
                if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                {
                    colorsToCheck.Add(neighbor.Color);
                    if (!availableColors.Contains(neighbor.Color))
                        availableColors.Add(neighbor.Color);
                }
            }

            // For each color, check if we have 3+ balls (including the rainbow)
            foreach (BallColor color in colorsToCheck)
            {
                // First, collect all direct neighbors of this color
                List<Ball> directNeighborsOfColor = new();
                foreach (Ball neighbor in neighbors)
                {
                    if (neighbor != null && neighbor.Color == color &&
                        neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                    {
                        directNeighborsOfColor.Add(neighbor);
                    }
                }

                if (directNeighborsOfColor.Count == 0) continue;

                //Debug.Log($"[RainbowBall] Checking color {color}: Found {directNeighborsOfColor.Count} direct neighbors");

                // Now find all connected balls of this color starting from these direct neighbors
                HashSet<Ball> colorGroup = new();
                Queue<Ball> toCheck = new();

                // Start flood fill from all direct neighbors of this color
                foreach (Ball directNeighbor in directNeighborsOfColor)
                {
                    toCheck.Enqueue(directNeighbor);
                }

                while (toCheck.Count > 0)
                {
                    Ball current = toCheck.Dequeue();
                    if (colorGroup.Contains(current)) continue;

                    colorGroup.Add(current);
                    //Debug.Log($"[RainbowBall] Added {current.Color} ball at {current.GridPosition} to color group");

                    // Check neighbors of same color (excluding the rainbow ball itself)
                    foreach (Ball n in current.Neighbors)
                    {
                        if (n != null && n != _ball && n.Color == color &&
                            n.HasFlag(BallFlags.Pinned) && !n.HasFlag(BallFlags.Destroying) &&
                            !colorGroup.Contains(n))
                        {
                            //Debug.Log($"[RainbowBall] Found connected {n.Color} ball at {n.GridPosition}, adding to queue");
                            toCheck.Enqueue(n);
                        }
                    }
                }

                //Debug.Log($"[RainbowBall] Color {color} group size: {colorGroup.Count} (need 2+ for match)");

                // If this color group + rainbow ball makes 3 or more, include them
                if (colorGroup.Count >= 2) // 2 color balls + 1 rainbow = 3 total
                {
                    //Debug.Log($"[RainbowBall] Valid match found! Adding {colorGroup.Count} balls to destruction list");
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
                else if (colorGroup.Count == 1)
                {
                    //Debug.Log($"[RainbowBall] Only 1 ball of color {color} - not enough for match (need 2+)");
                }
            }

            // If we found valid matches, add rainbow ball
            if (affectedBalls.Count > 0)
            {
                Debug.Log($"[RainbowBall] Total affected balls: {affectedBalls.Count}. Adding rainbow to destruction list.");
                affectedBalls.Add(_ball);
                _ball.Flags |= BallFlags.MarkedForMatch;
            }
            else
            {
                // No matches found - transform rainbow ball into a normal ball of a random neighboring color
                BallColor randomColor = BallColor.Blue;
                if (availableColors.Count > 0)
                {
                    randomColor = availableColors[Random.Range(0, availableColors.Count)];
                }
                else
                {
                    var gridColors = ColorTrackerManager.Instance.GetColorCounts();
                    if (gridColors.Count > 0)
                    {
                        Debug.Log($"[RainbowBall] No neighboring colors found. Picking random color from {gridColors.Count} grid colors");
                        var colorList = new List<BallColor>(gridColors.Keys);
                        randomColor = colorList[Random.Range(0, colorList.Count)];
                    }
                }

                // Only transform during actual detection (when we're using ball's actual neighbors, not preview)
                if (impactPosition == Vector2.zero || (Vector2)_ball.transform.position == impactPosition)
                {
                    Debug.Log($"[RainbowBall] No valid matches found. Transforming to random color from {availableColors.Count} available colors");

                    // Store rainbow ball's position data
                    Vector2Int gridPos = _ball.GridPosition;
                    Vector3 worldPos = _ball.transform.position;

                    // Get new normal ball from pool
                    GameObject newBallObj = ObjectPooler.Instance.Get(randomColor);
                    if (newBallObj != null)
                    {
                        Ball newBall = newBallObj.GetComponent<Ball>();
                        if (newBall != null)
                        {
                            // Configure at same position
                            newBall.transform.SetParent(gridManager.GridContainer);
                            newBall.transform.position = worldPos;
                            newBall.SetPosition(gridPos, worldPos);

                            // Update grid matrix
                            gridManager.Balls[gridPos.y][gridPos.x] = newBall;

                            // Track color
                            ColorTrackerManager.Instance.AddColor(randomColor);

                            // Copy neighbors from rainbow ball to new ball
                            newBall.UpdateNeighbors(_ball.Neighbors);

                            // Update all neighbors to point to the new ball instead of rainbow
                            foreach (Ball neighbor in _ball.Neighbors)
                            {
                                if (neighbor != null)
                                {
                                    // Get neighbor's current neighbors array
                                    Ball[] neighborNeighbors = neighbor.Neighbors;

                                    // Replace reference to rainbow ball with new ball
                                    for (int i = 0; i < neighborNeighbors.Length; i++)
                                    {
                                        if (neighborNeighbors[i] == _ball)
                                        {
                                            neighborNeighbors[i] = newBall;
                                        }
                                    }

                                    // Update the neighbor with modified array
                                    neighbor.UpdateNeighbors(neighborNeighbors);
                                }
                            }

                            // Destroy the rainbow ball
                            Destroy(gameObject);
                            SoundManager.Instance.StopSfxLoop();
                        }
                    }
                }
            }



            return affectedBalls;
        }

        /// <summary>
        /// Draw debug visualization for rainbow ball matches
        /// </summary>
        public override void DrawDebugVisualization(Vector2 impactPosition, Vector2 impactDirection = default)
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