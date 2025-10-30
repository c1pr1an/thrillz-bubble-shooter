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

        // Debug visualization for rocket runway
        private bool _showRocketRunway = false;
        private Vector2 _debugRunwayCenter;
        private Vector2 _debugRunwaySize;
        private float _debugRunwayAngle;

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

            // Get rocket component settings
            var rocketComponent = _currentBall.GetComponent<RocketBall>();
            if (rocketComponent == null || _trajectoryPredictor == null)
            {
                ClearAllHighlights();
                return;
            }

            // Get the last segment direction for accurate impact angle (handles bounces)
            Vector2 trajectoryDirection = _trajectoryPredictor.GetLastSegmentDirection();

            // Runway dimensions
            float ballDiameter = _gridManager.BallWidth;
            float runwayLength = _gridManager.BallHeight * 6f; // 6 balls forward
            float runwayWidth = ballDiameter * 1.5f; // 1.5 balls wide

            // Calculate the center of the runway (offset forward from impact position)
            Vector2 runwayCenter = impactPosition + (trajectoryDirection * runwayLength * 0.5f);

            // Calculate rotation angle for the box (aligned with trajectory)
            float angle = Mathf.Atan2(trajectoryDirection.y, trajectoryDirection.x) * Mathf.Rad2Deg;

            // Store debug info for visualization
            _showRocketRunway = true;
            _debugRunwayCenter = runwayCenter;
            _debugRunwaySize = new Vector2(runwayLength, runwayWidth);
            _debugRunwayAngle = angle;

            // Use OverlapBox to detect all balls in the runway area
            int ballLayer = LayerMask.NameToLayer("Default");
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(1 << ballLayer);
            contactFilter.useLayerMask = true;

            // Get all colliders in the runway area
            Collider2D[] hitColliders = new Collider2D[100];
            Vector2 boxSize = new Vector2(runwayLength, runwayWidth);
            int numHits = Physics2D.OverlapBox(runwayCenter, boxSize, angle, contactFilter, hitColliders);

            // Process all hit balls
            HashSet<Ball> affectedBalls = new HashSet<Ball>();
            for (int i = 0; i < numHits; i++)
            {
                if (hitColliders[i] != null)
                {
                    Ball ball = hitColliders[i].GetComponent<Ball>();

                    // Check if it's a valid ball (pinned and not already destroying)
                    if (ball != null && ball.HasFlag(BallFlags.Pinned) && !ball.HasFlag(BallFlags.Destroying))
                    {
                        // Additional check: make sure the ball is in front of the impact position
                        Vector2 toBall = (Vector2)ball.transform.position - impactPosition;
                        float dotProduct = Vector2.Dot(toBall.normalized, trajectoryDirection);

                        // Only include balls that are in front (dot product > 0)
                        if (dotProduct > 0)
                        {
                            affectedBalls.Add(ball);
                        }
                    }
                }
            }

            // Update highlights
            UpdateHighlights(affectedBalls);
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

            // If it's a rocket ball, show path based on physics detection
            if (_currentBall.IsRocket())
            {
                // Rocket balls don't use grid position, return empty
                // The highlighting is handled separately in UpdateHighlightsForRocket
                return previewList;
            }
            // If it's a lightning ball, show horizontal strike
            else if (_currentBall.IsLightning())
            {
                // Clear rocket runway debug when not showing rocket
                _showRocketRunway = false;
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
                // Clear rocket runway debug when not showing rocket
                _showRocketRunway = false;
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
                // Clear rocket runway debug when not showing rocket
                _showRocketRunway = false;
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
                // Clear rocket runway debug for other ball types
                _showRocketRunway = false;
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

        /// <summary>
        /// Draw debug visualization for rocket runway
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showRocketRunway) return;

            // Draw the overlap box that represents the rocket runway
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency

            // Convert angle to rotation matrix
            float angleRad = _debugRunwayAngle * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Vector2 up = new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));

            // Calculate the four corners of the rotated box
            Vector2 halfSize = _debugRunwaySize * 0.5f;
            Vector3[] corners = new Vector3[4];
            corners[0] = _debugRunwayCenter + right * halfSize.x + up * halfSize.y;
            corners[1] = _debugRunwayCenter - right * halfSize.x + up * halfSize.y;
            corners[2] = _debugRunwayCenter - right * halfSize.x - up * halfSize.y;
            corners[3] = _debugRunwayCenter + right * halfSize.x - up * halfSize.y;

            // Draw the box outline
            Gizmos.color = Color.red;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }

            // Draw diagonal lines to show it's a filled area
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawLine(corners[0], corners[2]);
            Gizmos.DrawLine(corners[1], corners[3]);

            // Draw center point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_debugRunwayCenter, 0.1f);

            // Draw direction arrow from center
            Vector3 arrowEnd = (Vector3)_debugRunwayCenter + (Vector3)(right * _debugRunwaySize.x * 0.4f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_debugRunwayCenter, arrowEnd);

            // Draw arrow head
            Vector3 arrowLeft = arrowEnd - (Vector3)(right * 0.2f) + (Vector3)(up * 0.2f);
            Vector3 arrowRight = arrowEnd - (Vector3)(right * 0.2f) - (Vector3)(up * 0.2f);
            Gizmos.DrawLine(arrowEnd, arrowLeft);
            Gizmos.DrawLine(arrowEnd, arrowRight);
        }
    }
}