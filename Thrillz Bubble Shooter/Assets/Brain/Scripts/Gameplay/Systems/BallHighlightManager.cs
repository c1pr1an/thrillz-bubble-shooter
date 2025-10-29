using System.Collections.Generic;
using UnityEngine;
using Brain.Util;
using Brain.Gameplay;
using Brain.Gameplay.Containers;
using Brain.Managers;

namespace Brain.Gameplay
{
    public class BallHighlightManager : UnitySingleton<BallHighlightManager>
    {
        // Private Fields
        [Header("Settings")]
        [SerializeField] private bool _enableHighlighting = true;

        private HashSet<Ball> _highlightedBalls = new HashSet<Ball>();
        private LaunchContainer _launchContainer;
        private TrajectoryPredictor _trajectoryPredictor;
        private GridManager _gridManager;
        private Vector2Int _lastPredictedGridPos = new Vector2Int(-1, -1);
        private Ball _currentBall;

        public void Init(GridManager gridManager, TrajectoryPredictor trajectoryPredictor)
        {
            _gridManager = gridManager;
            _launchContainer = gridManager.BallLaunchContainer;
            _trajectoryPredictor = trajectoryPredictor;
        }

        private void Update()
        {
            if (!_enableHighlighting)
                return;

            // Check if we have a ball in launch container
            if (_launchContainer == null || _launchContainer.CurrentBall == null)
            {
                if (_currentBall != null)
                {
                    ClearAllHighlights();
                    _currentBall = null;
                    _lastPredictedGridPos = new Vector2Int(-1, -1);
                }
                return;
            }

            // Check if ball changed
            if (_currentBall != _launchContainer.CurrentBall)
            {
                _currentBall = _launchContainer.CurrentBall;
                _lastPredictedGridPos = new Vector2Int(-1, -1);
            }

            // Check if trajectory prediction has a valid target
            if (_trajectoryPredictor == null || !_trajectoryPredictor.HasValidPrediction)
            {
                ClearAllHighlights();
                _lastPredictedGridPos = new Vector2Int(-1, -1);
                return;
            }

            // Find the grid position where the ball would actually land (nearest empty cell)
            Vector2 impactPoint = _trajectoryPredictor.PredictedImpactPosition;

            // First, find where the ball would snap to (nearest empty cell)
            Vector2Int gridPos = GridUtils.FindNearestEmptyCell(
                impactPoint,
                _gridManager.BallWidth,
                _gridManager.BallHeight,
                _gridManager.GridContainer,
                (x, y) => _gridManager.GetBall(x, y) == null
            );

            // If no valid position found, clear highlights
            if (gridPos.x < 0 || gridPos.y < 0)
            {
                ClearAllHighlights();
                _lastPredictedGridPos = new Vector2Int(-1, -1);
                return;
            }

            // Only update if the predicted grid position changed
            if (gridPos != _lastPredictedGridPos)
            {
                _lastPredictedGridPos = gridPos;
                UpdateHighlightsForGridPosition(gridPos);
            }
        }

        /// <summary>
        /// Update highlights for a specific grid position
        /// </summary>
        private void UpdateHighlightsForGridPosition(Vector2Int gridPos)
        {
            if (_currentBall == null)
            {
                ClearAllHighlights();
                return;
            }

            // Get neighbors at this position
            List<Ball> neighbors = GetNeighborsAtPosition(gridPos);

            // Use a modified approach for match preview
            List<Ball> matchPreview = GetMatchPreviewForPosition(gridPos, neighbors);

            // Convert to HashSet for the UpdateHighlights method
            HashSet<Ball> affectedBalls = new HashSet<Ball>(matchPreview);

            // Update highlights
            UpdateHighlights(affectedBalls);
        }

        /// <summary>
        /// Get all neighbor balls at a grid position
        /// </summary>
        private List<Ball> GetNeighborsAtPosition(Vector2Int gridPos)
        {
            List<Ball> neighbors = new List<Ball>();
            Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(gridPos);

            for (int i = 0; i < neighborPositions.Length; i++)
            {
                if (neighborPositions[i].HasValue)
                {
                    Ball neighbor = _gridManager.GetBall(neighborPositions[i].Value.x, neighborPositions[i].Value.y);
                    if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Calculate match preview without creating GameObjects
        /// </summary>
        private List<Ball> GetMatchPreviewForPosition(Vector2Int gridPos, List<Ball> neighbors)
        {
            List<Ball> previewList = new List<Ball>();

            if (!_currentBall.IsBonusBall)
            {
                // No highlights for regular balls
                return previewList;
            }

            // If it's a rocket ball, show path based on trajectory
            if (_currentBall.IsRocket())
            {
                // Get rocket component settings
                var rocketComponent = _currentBall.GetComponent<RocketBall>();
                if (rocketComponent != null)
                {
                    int ballsPerRow = rocketComponent.GetBallsPerRow();
                    int maxRows = rocketComponent.GetMaxRows();

                    // Calculate trajectory direction from current position to target
                    Vector2 currentPos = _currentBall.transform.position;
                    Vector2 targetPos = GridUtils.PosToWorld(gridPos, _gridManager.BallWidth, _gridManager.BallHeight, _gridManager.GridContainer);
                    Vector2 trajectoryDirection = (targetPos - currentPos).normalized;

                    // The rocket continues forward after landing - search in same direction
                    Vector2 searchDirection = trajectoryDirection;

                    // For each row distance forward from impact
                    for (int row = 1; row <= maxRows; row++)
                    {
                        List<Ball> ballsInRow = new List<Ball>();
                        float searchDistance = row * _gridManager.BallHeight;
                        Vector2 rowCenter = targetPos + searchDirection * searchDistance;

                        // Find balls near this row position
                        float rowTolerance = _gridManager.BallHeight * 0.6f;

                        // Check all balls in the grid
                        for (int y = 0; y < _gridManager.MaxRows; y++)
                        {
                            for (int x = 0; x < GridUtils.GetMaxColumns(y); x++)
                            {
                                Ball ball = _gridManager.GetBall(x, y);
                                if (ball != null && ball.HasFlag(BallFlags.Pinned) && !ball.HasFlag(BallFlags.Destroying))
                                {
                                    // Check if this ball is roughly at the right distance
                                    float distanceFromTarget = Vector2.Distance(ball.transform.position, targetPos);
                                    float expectedDistance = row * _gridManager.BallHeight;

                                    if (Mathf.Abs(distanceFromTarget - expectedDistance) <= rowTolerance)
                                    {
                                        // Check if the ball is in the search direction
                                        Vector2 toBall = (Vector2)ball.transform.position - targetPos;
                                        float dotProduct = Vector2.Dot(toBall.normalized, searchDirection);

                                        if (dotProduct > 0.5f)
                                        {
                                            // Calculate perpendicular distance from the direction line
                                            Vector2 projection = Vector2.Dot(toBall, searchDirection) * searchDirection;
                                            Vector2 perpendicular = toBall - projection;
                                            float perpendicularDistance = perpendicular.magnitude;

                                            if (perpendicularDistance <= _gridManager.BallWidth * 1.5f)
                                            {
                                                ballsInRow.Add(ball);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Sort balls by distance from the direction line (closest first)
                        ballsInRow.Sort((a, b) =>
                        {
                            Vector2 toA = (Vector2)a.transform.position - targetPos;
                            Vector2 projA = Vector2.Dot(toA, searchDirection) * searchDirection;
                            float distA = (toA - projA).magnitude;

                            Vector2 toB = (Vector2)b.transform.position - targetPos;
                            Vector2 projB = Vector2.Dot(toB, searchDirection) * searchDirection;
                            float distB = (toB - projB).magnitude;

                            return distA.CompareTo(distB);
                        });

                        // Take up to ballsPerRow balls from this row
                        int ballsToTake = Mathf.Min(ballsInRow.Count, ballsPerRow);
                        for (int i = 0; i < ballsToTake; i++)
                        {
                            previewList.Add(ballsInRow[i]);
                        }
                    }
                }
            }
            // If it's a lightning ball, show horizontal strike
            else if (_currentBall.IsLightning())
            {
                // Get lightning component to check range
                var lightningComponent = _currentBall.GetComponent<LightningBall>();
                int horizontalRange = lightningComponent != null ? lightningComponent.GetHorizontalRange() : 4;

                // Collect balls to the left
                for (int x = gridPos.x - 1; x >= gridPos.x - horizontalRange; x--)
                {
                    if (!GridUtils.IsValidPosition(x, gridPos.y))
                        break;

                    Ball ballAtPos = _gridManager.GetBall(x, gridPos.y);
                    if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                    {
                        previewList.Add(ballAtPos);
                    }
                }

                // Collect balls to the right
                for (int x = gridPos.x + 1; x <= gridPos.x + horizontalRange; x++)
                {
                    if (!GridUtils.IsValidPosition(x, gridPos.y))
                        break;

                    Ball ballAtPos = _gridManager.GetBall(x, gridPos.y);
                    if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                    {
                        previewList.Add(ballAtPos);
                    }
                }
            }
            // If it's a bomb ball, show explosion radius
            else if (_currentBall.IsBomb())
            {
                // Get bomb component to check radius
                var bombComponent = _currentBall.GetComponent<BombBall>();
                int explosionRadius = bombComponent != null ? bombComponent.GetExplosionRadius() : 2;

                // Get all positions within explosion radius
                List<Vector2Int> explosionPositions = GridUtils.GetExtendedNeighborPositions(gridPos, explosionRadius);

                // Add all balls at these positions to preview
                foreach (Vector2Int pos in explosionPositions)
                {
                    Ball ballAtPos = _gridManager.GetBall(pos.x, pos.y);
                    if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                    {
                        previewList.Add(ballAtPos);
                    }
                }
            }
            // If it's a rainbow ball, handle special logic
            else if (_currentBall.IsRainbow())
            {
                // Track which colors to check
                HashSet<BallColor> colorsToCheck = new HashSet<BallColor>();
                HashSet<Ball> processedBalls = new HashSet<Ball>();

                // Identify all colors directly touching the landing position
                foreach (Ball neighbor in neighbors)
                {
                    colorsToCheck.Add(neighbor.Color);
                }

                // For each color, check if we have 3+ balls (including the rainbow)
                foreach (BallColor color in colorsToCheck)
                {
                    List<Ball> colorGroup = new List<Ball>();

                    // Find a starting neighbor of this color
                    Ball startBall = null;
                    foreach (Ball neighbor in neighbors)
                    {
                        if (neighbor.Color == color)
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

                    // If this color group + rainbow makes 3 or more, include them
                    if (colorGroup.Count >= 2) // 2 color balls + 1 rainbow = 3 total
                    {
                        foreach (Ball ball in colorGroup)
                        {
                            if (!processedBalls.Contains(ball))
                            {
                                previewList.Add(ball);
                                processedBalls.Add(ball);
                            }
                        }
                    }
                }
            }

            return previewList;
        }


        /// <summary>
        /// Update which balls are highlighted
        /// </summary>
        private void UpdateHighlights(HashSet<Ball> newHighlights)
        {
            // Clear old highlights not in new set
            HashSet<Ball> toClear = new HashSet<Ball>(_highlightedBalls);
            toClear.ExceptWith(newHighlights);

            foreach (var ball in toClear)
            {
                if (ball != null)
                    ball.SetHighlight(false);
            }

            // Add new highlights
            foreach (var ball in newHighlights)
            {
                if (ball != null)
                    ball.SetHighlight(true);
            }

            _highlightedBalls = newHighlights;
        }

        /// <summary>
        /// Clear all ball highlights
        /// </summary>
        private void ClearAllHighlights()
        {
            foreach (var ball in _highlightedBalls)
            {
                if (ball != null)
                    ball.SetHighlight(false);
            }

            _highlightedBalls.Clear();
        }

        private void OnDisable()
        {
            ClearAllHighlights();
        }

        private void OnDestroy()
        {
            ClearAllHighlights();
        }
    }
}