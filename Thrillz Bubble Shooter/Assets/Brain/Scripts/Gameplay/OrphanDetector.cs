using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brain.Managers;
using Brain.Util;
using UnityEngine;

namespace Brain.Gameplay
{
    public class OrphanDetector : UnitySingleton<OrphanDetector>
    {
        private float _delayBetweenFalls = 0.02f;

        private HashSet<Ball> _connectedBalls = new HashSet<Ball>();
        private bool _isChecking = false;
        private bool _isAnimating = false;

        public bool IsChecking() => _isChecking;
        public bool IsAnimating() => _isAnimating;

        public void CheckSeparatedBalls()
        {
            StartCoroutine(CheckSeparatedBallsCoroutine());
        }

        private IEnumerator CheckSeparatedBallsCoroutine()
        {
            _isChecking = true;
            _isAnimating = true;

            // Find ALL orphaned balls in a single pass
            List<Ball> ballsToFall = FindOrphanedBalls();

            if (ballsToFall.Count > 0)
            {
                // Sort by row (highest rows fall first for better visual effect)
                ballsToFall = ballsToFall.OrderByDescending(ball => -ball.GridPosition.y).ToList();

                // Remove from grid immediately (logic update)
                foreach (Ball ball in ballsToFall)
                {
                    if (ball != null)
                    {
                        GridManager.Instance.RemoveBall(ball);
                    }
                }
            }

            // Logic detection is complete, set flag to false
            _isChecking = false;

            // Count falling balls for bonus power (excluding rainbow balls)
            int fallingCount = 0;
            foreach (Ball ball in ballsToFall)
            {
                if (ball != null && !ball.IsRainbow())
                {
                    fallingCount++;
                }
            }

            // Add power for falling balls
            if (fallingCount > 0)
            {
                BonusPowerManager.Instance.AddPower(fallingCount);
            }

            // Handle the animations
            foreach (Ball ball in ballsToFall)
            {
                if (ball != null)
                {
                    ball.Fall();
                    yield return new WaitForSeconds(_delayBetweenFalls);
                }
            }

            _isAnimating = false;
        }

        private List<Ball> FindOrphanedBalls()
        {
            GridManager gridManager = GridManager.Instance;

            _connectedBalls.Clear();

            gridManager.ClearAllMarks();

            foreach (Ball rootBall in Ball.s_rootBalls)
            {
                if (rootBall != null && rootBall.HasFlag(BallFlags.Pinned) && !rootBall.HasFlag(BallFlags.Destroying))
                {
                    FindConnectedBalls(rootBall);
                }
            }

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

            ball.Flags |= BallFlags.MarkConnected;
            _connectedBalls.Add(ball);

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
