using System.Collections.Generic;
using UnityEngine;
using Brain.Util;
using Brain.Gameplay;
using Brain.Gameplay.Containers;

namespace Brain.Managers
{
    public class BallHighlightManager : UnitySingleton<BallHighlightManager>
    {
        // Private Fields
        [Header("Settings")]
        [SerializeField] private bool _enableHighlighting = true;

        private HashSet<Ball> _highlightedBalls = new HashSet<Ball>();
        private LaunchContainer _launchContainer;
        private BonusBallContainer _bonusBallContainer;
        private TrajectoryPredictor _trajectoryPredictor;
        private GridManager _gridManager;
        private Vector2Int _lastPredictedGridPos = new Vector2Int(-1, -1);
        private Ball _currentBall;
        private bool _isAiming = false;

        public void Init(GridManager gridManager, TrajectoryPredictor trajectoryPredictor, BonusBallContainer bonusBallContainer = null)
        {
            _gridManager = gridManager;
            _launchContainer = gridManager.BallLaunchContainer;
            _trajectoryPredictor = trajectoryPredictor;
            _bonusBallContainer = bonusBallContainer;
        }

        private void OnEnable()
        {
            // Subscribe to InputManager aiming events
            InputManager.OnAimingStarted += OnAimingStarted;
            InputManager.OnAimingUpdated += OnAimingUpdated;
            InputManager.OnAimingReleased += OnAimingReleased;
            InputManager.OnAimingCancelled += OnAimingCancelled;
        }

        private void OnAimingStarted(Vector2 position)
        {
            _isAiming = true;
        }

        private void OnAimingUpdated(Vector2 position)
        {
            _isAiming = true;
        }

        private void OnAimingReleased(Vector2 position)
        {
            _isAiming = false;
            ClearAllHighlights();
            _lastPredictedGridPos = new Vector2Int(-1, -1);
        }

        private void OnAimingCancelled()
        {
            _isAiming = false;
            ClearAllHighlights();
            _lastPredictedGridPos = new Vector2Int(-1, -1);
        }

        private void Update()
        {
            if (!_enableHighlighting || !_isAiming)
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

            // Only highlight for bonus balls
            if (_currentBall == null || !_currentBall.IsBonusBall)
            {
                if (_highlightedBalls.Count > 0)
                {
                    ClearAllHighlights();
                    _lastPredictedGridPos = new Vector2Int(-1, -1);
                }
                return;
            }

            // Check if trajectory prediction has a valid target
            if (_trajectoryPredictor == null || !_trajectoryPredictor.HasValidPrediction)
            {
                ClearAllHighlights();
                _lastPredictedGridPos = new Vector2Int(-1, -1);
                return;
            }

            // Find the grid position where the ball would actually land
            Vector2 impactPoint = _trajectoryPredictor.PredictedImpactPosition;

            // For rocket balls, we need to update more frequently since they don't snap to grid
            if (_currentBall != null && _currentBall.IsRocket())
            {
                // For rockets, update based on actual impact position changes
                // Use a small threshold to avoid constant updates from tiny movements
                Vector2 impactDifference = impactPoint - (Vector2)GridUtils.PosToWorld(_lastPredictedGridPos, _gridManager.BallWidth, _gridManager.BallHeight, _gridManager.GridContainer);

                if (impactDifference.magnitude > 0.1f || _lastPredictedGridPos.x < 0)
                {
                    // Use approximate grid position for tracking, but update based on actual position
                    Vector2Int approximateGridPos = GridUtils.WorldToPos(impactPoint, _gridManager.BallWidth, _gridManager.BallHeight, _gridManager.GridContainer);
                    _lastPredictedGridPos = approximateGridPos;
                    UpdateHighlightsForRocket(impactPoint);
                }
            }
            else
            {
                // For regular balls, find where they would snap to (nearest empty cell)
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
        }

        /// <summary>
        /// Update highlights for rocket ball using actual impact position
        /// </summary>
        private void UpdateHighlightsForRocket(Vector2 impactPosition)
        {
            if (_currentBall == null || !_currentBall.IsRocket())
            {
                ClearAllHighlights();
                return;
            }

            // Get rocket component
            var rocketComponent = _currentBall.GetComponent<RocketBall>();
            if (rocketComponent == null || _trajectoryPredictor == null)
            {
                ClearAllHighlights();
                return;
            }

            // Get the last segment direction for accurate impact angle (handles bounces)
            Vector2 trajectoryDirection = _trajectoryPredictor.GetLastSegmentDirection();

            // Draw debug visualization (RocketBall handles this now)
            rocketComponent.DrawDebugVisualization(impactPosition, trajectoryDirection);

            // Get affected balls from the rocket component
            List<Ball> affectedBalls = rocketComponent.GetAffectedBalls(impactPosition, trajectoryDirection);

            // Update highlights
            UpdateHighlights(new HashSet<Ball>(affectedBalls));
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

            // If it's a rocket ball, return empty (highlighting is handled separately in UpdateHighlightsForRocket)
            if (_currentBall.IsRocket())
            {
                return previewList;
            }

            // Use BonusBallBase for all other bonus balls
            BonusBallBase detector = _currentBall.GetComponent<BonusBallBase>();
            if (detector != null)
            {
                // Convert grid position to world position for the detector
                Vector2 worldPosition = GridUtils.PosToWorld(gridPos, _gridManager.BallWidth, _gridManager.BallHeight, _gridManager.GridContainer);

                // Get affected balls from the bonus ball detector
                List<Ball> affectedBalls = detector.GetAffectedBalls(worldPosition);

                // Remove the current ball itself from the preview (it hasn't landed yet)
                affectedBalls.Remove(_currentBall);

                previewList = affectedBalls;
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
                if (ball != null && !IsBallInContainer(ball))
                {
                    ball.SetHighlight(false);
                    // Clear the MarkedForMatch flag when no longer highlighted
                    ball.Flags &= ~BallFlags.MarkedForMatch;
                }
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
        /// Clear all ball highlights (except bonus balls in containers)
        /// </summary>
        private void ClearAllHighlights()
        {
            foreach (var ball in _highlightedBalls)
            {
                if (ball != null && !IsBallInContainer(ball))
                {
                    ball.SetHighlight(false);
                    // Clear the MarkedForMatch flag that bonus balls set during preview
                    ball.Flags &= ~BallFlags.MarkedForMatch;
                }
            }

            _highlightedBalls.Clear();
        }

        /// <summary>
        /// Check if a ball is in a container (bonus container or launch container)
        /// These balls should keep their highlights independently
        /// </summary>
        private bool IsBallInContainer(Ball ball)
        {
            if (ball == null)
                return false;

            // Check if it's in the bonus container
            if (_bonusBallContainer != null && _bonusBallContainer.CurrentBall == ball)
                return true;

            // Check if it's in the launch container (don't clear highlight of ball we're aiming with)
            if (_launchContainer != null && _launchContainer.CurrentBall == ball)
                return true;

            return false;
        }

        private void OnDisable()
        {
            // Unsubscribe from InputManager events
            InputManager.OnAimingStarted -= OnAimingStarted;
            InputManager.OnAimingUpdated -= OnAimingUpdated;
            InputManager.OnAimingReleased -= OnAimingReleased;
            InputManager.OnAimingCancelled -= OnAimingCancelled;

            ClearAllHighlights();
        }

        private void OnDestroy()
        {
            ClearAllHighlights();
        }
    }
}
