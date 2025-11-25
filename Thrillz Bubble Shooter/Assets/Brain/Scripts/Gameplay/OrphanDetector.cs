using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brain.Managers;
using Brain.Util;

namespace Brain.Gameplay
{
    public class OrphanDetector : UnitySingleton<OrphanDetector>
    {

        private HashSet<Ball> _connectedBalls = new HashSet<Ball>();
        private bool _isChecking = false;
        private bool _isAnimating = false;
        private List<Ball> _lastOrphanedBalls = new List<Ball>();

        public bool IsChecking() => _isChecking;
        public bool IsAnimating() => _isAnimating;
        public List<Ball> LastOrphanedBalls => _lastOrphanedBalls;

        public void CheckSeparatedBalls()
        {
            StartCoroutine(CheckSeparatedBallsCoroutine());
        }

        // Just detect orphans, don't animate them yet
        private IEnumerator CheckSeparatedBallsCoroutine()
        {
            _isChecking = true;

            // Find ALL orphaned balls in a single pass
            List<Ball> ballsToFall = FindOrphanedBalls();

            // Store for external access
            _lastOrphanedBalls = ballsToFall;

            if (ballsToFall.Count > 0)
            {
                // Sort by row (highest rows fall first for better visual effect)
                _lastOrphanedBalls = _lastOrphanedBalls.OrderByDescending(ball => -ball.GridPosition.y).ToList();
            }

            // Logic detection is complete, set flag to false
            _isChecking = false;

            yield return null; // Just to make it a coroutine
        }

        // Separate method to animate the falling of orphan balls
        public void AnimateOrphanFalling(int orphanScoreValue = -1)
        {
            if (_lastOrphanedBalls.Count > 0)
            {
                StartCoroutine(AnimateOrphanFallingCoroutine(orphanScoreValue));
            }
        }

        private IEnumerator AnimateOrphanFallingCoroutine(int orphanScoreValue = -1)
        {
            _isAnimating = true;

            // Remove all orphan balls from the grid (they're already marked as falling in MatchDetector)
            foreach (Ball ball in _lastOrphanedBalls)
            {
                if (ball != null)
                {
                    GridManager.Instance.RemoveBall(ball);
                }
            }

            // Count falling balls for bonus power (excluding rainbow balls)
            int fallingCount = 0;
            foreach (Ball ball in _lastOrphanedBalls)
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

            // Use passed score value or get from current streak if not provided
            if (orphanScoreValue == -1)
            {
                orphanScoreValue = ScoreManager.Instance.GetCurrentOrphanScore();
            }

            // Make all balls fall at once with no delay
            foreach (Ball ball in _lastOrphanedBalls)
            {
                if (ball != null)
                {
                    // Add streak-based points for each orphan ball
                    if (!ball.IsBonusBall)
                    {
                        ScoreManager.Instance.AddBubblePopScore(ball.transform.position, orphanScoreValue, false);
                    }

                    ball.Fall();
                }
            }

            // Small yield to ensure all physics are applied
            yield return null;

            // Clear the list after animating
            _lastOrphanedBalls.Clear();

            _isAnimating = false;
        }

        private List<Ball> FindOrphanedBalls()
        {
            GridManager gridManager = GridManager.Instance;

            _connectedBalls.Clear();

            gridManager.ClearAllMarks();

            foreach (Ball rootBall in Ball.s_rootBalls)
            {
                if (rootBall != null && rootBall.HasFlag(BallFlags.Pinned) &&
                    !rootBall.HasFlag(BallFlags.Destroying) &&
                    !rootBall.HasFlag(BallFlags.MarkedForDestroy))
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
            if (ball.HasFlag(BallFlags.MarkedForDestroy)) return; // Skip balls marked for destruction
            if (ball.HasFlag(BallFlags.MarkConnected)) return;

            ball.Flags |= BallFlags.MarkConnected;
            _connectedBalls.Add(ball);

            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null && !neighbor.HasFlag(BallFlags.MarkedForDestroy))
                {
                    FindConnectedBalls(neighbor);
                }
            }
        }
    }
}
