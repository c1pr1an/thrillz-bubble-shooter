using System.Collections.Generic;
using UnityEngine;
using Brain.Util;
using Brain.Gameplay;
using Brain.Gameplay.Containers;
using Brain.Managers;

namespace Brain.Gameplay
{
    /// <summary>
    /// Manages highlighting of balls that will be affected by bonus balls
    /// </summary>
    public class BallHighlightManager : UnitySingleton<BallHighlightManager>
    {
        [Header("Settings")]
        [SerializeField] private bool _enableHighlighting = true;
        [SerializeField] private float _updateInterval = 0.1f; // Update frequency

        private float _lastUpdateTime;
        private HashSet<Ball> _highlightedBalls = new HashSet<Ball>();
        private LaunchContainer _launchContainer;
        private TrajectoryPredictor _trajectoryPredictor;
        private GridManager _gridManager;

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

            // Throttle updates
            if (Time.time - _lastUpdateTime < _updateInterval)
                return;

            _lastUpdateTime = Time.time;

            // Check if we have a rainbow ball in launch container
            if (_launchContainer == null || _launchContainer.CurrentBall == null)
            {
                ClearAllHighlights();
                return;
            }

            Ball currentBall = _launchContainer.CurrentBall;
            if (!currentBall.IsRainbow())
            {
                ClearAllHighlights();
                return;
            }

            // Update highlights for rainbow ball
            UpdateRainbowHighlights(currentBall);
        }

        /// <summary>
        /// Update highlights for Rainbow ball
        /// </summary>
        private void UpdateRainbowHighlights(Ball rainbowBall)
        {
            // Get predicted impact position
            if (_trajectoryPredictor == null || !_trajectoryPredictor.HasValidPrediction)
            {
                ClearAllHighlights();
                return;
            }

            Vector2 impactPoint = _trajectoryPredictor.PredictedImpactPosition;

            // Find balls that would be affected at impact point
            HashSet<Ball> affectedBalls = GetRainbowAffectedBalls(impactPoint);

            // Update highlights
            UpdateHighlights(affectedBalls);
        }

        /// <summary>
        /// Get all balls that would be affected by Rainbow ball at impact
        /// </summary>
        private HashSet<Ball> GetRainbowAffectedBalls(Vector2 impactPoint)
        {
            HashSet<Ball> affected = new HashSet<Ball>();

            // Find the closest ball to impact point
            Ball closestBall = FindClosestBall(impactPoint);
            if (closestBall == null)
                return affected;

            // Rainbow ball affects all adjacent balls of ANY color
            // First, add the impact ball
            affected.Add(closestBall);

            // Then find all connected balls of each color touching the impact
            HashSet<Ball> processed = new HashSet<Ball>();
            Queue<Ball> toProcess = new Queue<Ball>();

            // Add all neighbors of impact ball to process
            foreach (var neighbor in closestBall.Neighbors)
            {
                if (neighbor != null && !neighbor.HasFlag(BallFlags.Destroying))
                {
                    toProcess.Enqueue(neighbor);
                }
            }

            // Process each color group
            while (toProcess.Count > 0)
            {
                Ball ball = toProcess.Dequeue();
                if (processed.Contains(ball))
                    continue;

                processed.Add(ball);
                affected.Add(ball);

                // Add same-color neighbors (flood fill per color)
                foreach (var neighbor in ball.Neighbors)
                {
                    if (neighbor != null &&
                        !processed.Contains(neighbor) &&
                        !neighbor.HasFlag(BallFlags.Destroying) &&
                        neighbor.Color == ball.Color)
                    {
                        toProcess.Enqueue(neighbor);
                    }
                }
            }

            return affected;
        }

        /// <summary>
        /// Find the closest ball to a world position
        /// </summary>
        private Ball FindClosestBall(Vector2 worldPos)
        {
            if (_gridManager == null || _gridManager.Balls == null)
                return null;

            Ball closest = null;
            float minDistance = float.MaxValue;

            // Iterate through the 2D list of balls
            foreach (var row in _gridManager.Balls)
            {
                if (row == null) continue;

                foreach (var ball in row)
                {
                    if (ball == null || ball.HasFlag(BallFlags.Destroying))
                        continue;

                    float distance = Vector2.Distance(ball.transform.position, worldPos);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closest = ball;
                    }
                }
            }

            // Only return if close enough (within ball radius)
            if (minDistance < 1f) // Adjust threshold as needed
                return closest;

            return null;
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