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

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            // Cache references
            _gridManager = GridManager.Instance;

            if (_gridManager != null)
            {
                _launchContainer = _gridManager.BallLaunchContainer;
            }

            _trajectoryPredictor = TrajectoryPredictor.Instance;
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

            // If it's a rainbow ball, handle special logic
            if (_currentBall.IsRainbow())
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
            else
            {
                // Regular ball - check for matches with same color neighbors
                List<Ball> sameColorNeighbors = new List<Ball>();
                foreach (Ball neighbor in neighbors)
                {
                    if (neighbor.Color == _currentBall.Color)
                    {
                        sameColorNeighbors.Add(neighbor);
                    }
                }

                // If we have same-color neighbors, check for matches
                if (sameColorNeighbors.Count > 0)
                {
                    HashSet<Ball> allConnected = new HashSet<Ball>();

                    // Find all connected balls of the same color
                    foreach (Ball startBall in sameColorNeighbors)
                    {
                        Queue<Ball> toCheck = new Queue<Ball>();
                        toCheck.Enqueue(startBall);

                        while (toCheck.Count > 0)
                        {
                            Ball current = toCheck.Dequeue();
                            if (allConnected.Contains(current)) continue;

                            if (current.Color == _currentBall.Color &&
                                current.HasFlag(BallFlags.Pinned) && !current.HasFlag(BallFlags.Destroying))
                            {
                                allConnected.Add(current);

                                foreach (Ball n in current.Neighbors)
                                {
                                    if (n != null && !allConnected.Contains(n))
                                    {
                                        toCheck.Enqueue(n);
                                    }
                                }
                            }
                        }
                    }

                    // Only highlight if we'd have 3+ matches (including the new ball)
                    if (allConnected.Count >= 2) // 2 existing + 1 new = 3
                    {
                        previewList.AddRange(allConnected);
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