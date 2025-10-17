using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    /// <summary>
    /// Detects orphaned balls (not connected to top) and makes them fall
    /// Uses flood-fill from root balls to find connected component
    /// </summary>
    public class SeparatingBallManager : UnitySingleton<SeparatingBallManager>
    {
        [Header("Settings")]
        [SerializeField] private int maxIterations = 5; // Check multiple times for cascading falls
        [SerializeField] private float delayBetweenFalls = 0.05f;

        private HashSet<Ball> connectedBalls = new HashSet<Ball>();

        /// <summary>
        /// Checks for separated (orphaned) balls and makes them fall
        /// </summary>
        public void CheckSeparatedBalls()
        {
            StartCoroutine(CheckSeparatedBallsCoroutine());
        }

        private IEnumerator CheckSeparatedBallsCoroutine()
        {
            // Check multiple times for cascading effects
            for (int iteration = 0; iteration < maxIterations; iteration++)
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

                        yield return new WaitForSeconds(delayBetweenFalls);
                    }
                }

                // Wait a bit before next iteration
                yield return new WaitForSeconds(0.2f);
            }
        }

        /// <summary>
        /// Finds all balls that are not connected to the root (top row)
        /// Returns list of orphaned balls that should fall
        /// </summary>
        private List<Ball> FindOrphanedBalls()
        {
            GridManager gridManager = GridManager.Instance;

            // Clear previous results
            connectedBalls.Clear();

            // Clear all marks
            gridManager.ClearAllMarks();

            // Flood-fill from all root balls (top row)
            foreach (Ball rootBall in Ball.RootBalls)
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
                        !connectedBalls.Contains(ball))
                    {
                        orphanedBalls.Add(ball);
                    }
                }
            }

            return orphanedBalls;
        }

        /// <summary>
        /// Recursive flood-fill to find all balls connected to the given ball
        /// </summary>
        private void FindConnectedBalls(Ball ball)
        {
            if (ball == null) return;
            if (connectedBalls.Contains(ball)) return;
            if (!ball.HasFlag(BallFlags.Pinned)) return;
            if (ball.HasFlag(BallFlags.Destroying)) return;
            if (ball.HasFlag(BallFlags.MarkConnected)) return;

            // Mark as connected
            ball.Flags |= BallFlags.MarkConnected;
            connectedBalls.Add(ball);

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
