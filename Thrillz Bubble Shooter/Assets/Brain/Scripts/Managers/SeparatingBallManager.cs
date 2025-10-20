using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class SeparatingBallManager : UnitySingleton<SeparatingBallManager>
    {
        // Serialized Fields
        [Header("Settings")]
        [SerializeField] private int _maxIterations = 5;
        [SerializeField] private float _delayBetweenFalls = 0.05f;

        // Private Fields
        private HashSet<Ball> _connectedBalls = new HashSet<Ball>();

        // Public Methods
        public void CheckSeparatedBalls()
        {
            StartCoroutine(CheckSeparatedBallsCoroutine());
        }

        // Private Methods - Coroutine to check multiple times for cascading effects
        private IEnumerator CheckSeparatedBallsCoroutine()
        {
            for (int iteration = 0; iteration < _maxIterations; iteration++)
            {
                List<Ball> ballsToFall = FindOrphanedBalls();

                if (ballsToFall.Count == 0)
                {
                    // No more orphaned balls found
                    break;
                }

                // Sort by row (bottom to top) for visual effect
                ballsToFall = ballsToFall.OrderByDescending(ball => ball.Position.y).ToList();

                // Make balls fall with small delays
                foreach (Ball ball in ballsToFall)
                {
                    if (ball != null)
                    {
                        // Remove from grid
                        GridManager.Instance.RemoveBall(ball);

                        // Trigger fall
                        ball.Fall();

                        yield return new WaitForSeconds(_delayBetweenFalls);
                    }
                }

                // Wait a bit before next iteration
                yield return new WaitForSeconds(0.2f);
            }
        }

        private List<Ball> FindOrphanedBalls()
        {
            GridManager gridManager = GridManager.Instance;

            // Clear previous results
            _connectedBalls.Clear();

            // Clear all marks
            gridManager.ClearAllMarks();

            // Flood-fill from all root balls (top row)
            foreach (Ball rootBall in Ball.s_rootBalls)
            {
                if (rootBall != null && rootBall.HasFlag(BallFlags.Pinned) && !rootBall.HasFlag(BallFlags.Destroying))
                {
                    FindConnectedBalls(rootBall);
                }
            }

            // Find orphaned balls (pinned but not connected to root)
            List<Ball> orphanedBalls = new List<Ball>();
            var balls = gridManager.Balls;

            for (int row = 0; row < balls.Count; row++)
            {
                for (int col = 0; col < balls[row].Count; col++)
                {
                    Ball ball = balls[row][col];

                    if (ball != null &&
                        ball.HasFlag(BallFlags.Pinned) &&
                        !ball.HasFlag(BallFlags.MarkedForDestroy) &&
                        !ball.HasFlag(BallFlags.Destroying) &&
                        !_connectedBalls.Contains(ball))
                    {
                        orphanedBalls.Add(ball);
                    }
                }
            }

            return orphanedBalls;
        }

        private void FindConnectedBalls(Ball ball)
        {
            if (ball == null) return;
            if (_connectedBalls.Contains(ball)) return;
            if (!ball.HasFlag(BallFlags.Pinned)) return;
            if (ball.HasFlag(BallFlags.Destroying)) return;
            if (ball.HasFlag(BallFlags.MarkConnected)) return;

            // Mark as connected
            ball.Flags |= BallFlags.MarkConnected;
            _connectedBalls.Add(ball);

            // Recursively check neighbors
            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null)
                {
                    FindConnectedBalls(neighbor);
                }
            }
        }
    }
}
