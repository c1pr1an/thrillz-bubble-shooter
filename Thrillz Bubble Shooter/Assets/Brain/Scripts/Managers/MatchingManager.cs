using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    /// <summary>
    /// Manages match detection using flood-fill algorithm
    /// Finds 3+ connected balls of the same color
    /// </summary>
    public class MatchingManager : UnitySingleton<MatchingManager>
    {
        [Header("Settings")]
        [SerializeField] private int minMatchCount = 3;

        private List<Ball> matchList = new List<Ball>();

        /// <summary>
        /// Main entry point - called when a ball stops on the grid
        /// Processes match detection, destruction, and orphan detection
        /// </summary>
        public void ProcessBallStopped(Ball stoppedBall)
        {
            if (stoppedBall == null) return;

            // 1. Check for matches
            int matchCount = CheckMatch(stoppedBall);

            if (matchCount >= minMatchCount)
            {
                // 2. Destroy matched balls
                if (DestroyManager.Exists())
                {
                    DestroyManager.Instance.DestroyBalls(matchList);
                }
            }

            // 3. Check for orphaned balls (separated from root)
            if (SeparatingBallManager.Exists())
            {
                SeparatingBallManager.Instance.CheckSeparatedBalls();
            }
        }

        /// <summary>
        /// Checks for matches starting from the given ball
        /// Returns the number of matching balls found
        /// </summary>
        public int CheckMatch(Ball ball)
        {
            if (ball == null) return 0;

            // Clear previous match list
            matchList.Clear();

            // Start flood-fill from this ball
            FindMatches(ball, ball.Color);

            // Clear marks after checking
            ClearMarks();

            return matchList.Count;
        }

        /// <summary>
        /// Recursive flood-fill to find all connected balls of same color
        /// </summary>
        private void FindMatches(Ball ball, BallColor targetColor)
        {
            if (ball == null) return;

            // Skip if already marked or wrong color or not pinned
            if (ball.HasFlag(BallFlags.MarkedForMatch)) return;
            if (ball.Color != targetColor) return;
            if (!ball.HasFlag(BallFlags.Pinned)) return;
            if (ball.HasFlag(BallFlags.Destroying)) return;

            // Mark this ball as checked
            ball.Flags |= BallFlags.MarkedForMatch;
            matchList.Add(ball);

            // Recursively check all neighbors
            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null)
                {
                    FindMatches(neighbor, targetColor);
                }
            }
        }

        /// <summary>
        /// Clears MarkedForMatch flags from all balls in match list
        /// </summary>
        private void ClearMarks()
        {
            foreach (Ball ball in matchList)
            {
                if (ball != null)
                {
                    ball.Flags &= ~BallFlags.MarkedForMatch;
                }
            }
        }

        /// <summary>
        /// Returns a list of matched balls without triggering destruction
        /// Useful for preview or AI logic
        /// </summary>
        public List<Ball> GetMatchListPreview(Ball ball)
        {
            if (ball == null) return new List<Ball>();

            matchList.Clear();
            FindMatches(ball, ball.Color);
            ClearMarks();

            return new List<Ball>(matchList);
        }
    }
}
